/*
 * Copyright (c) 2006, Adam Dunkels
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. Neither the name of the author nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR AND CONTRIBUTORS ``AS IS'' AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 * OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 *
 */

using log4net;
using System;
using System.Collections.Generic;
using System.Collections;
using uBasicLibrary;
using System.Diagnostics;

namespace Dartmouth2
{
    /// <summary>
    /// Dartmouth basic version 2 - Oct 68 
    /// </summary>
    public class Interpreter : IInterpreter
    {
        #region Variables

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        readonly IConsoleIO consoleIO;

        int program_ptr;
        const int MAX_STRINGLEN = 40;

        // Printing

        const int ZONE_WIDTH = 15;
        const int COMPACT_WIDTH = 3;

        // Gosub

        const int MAX_GOSUB_STACK_DEPTH = 10;
        readonly int[] gosubStack = new int[MAX_GOSUB_STACK_DEPTH];
        int gosubStackPtr;

        // for-next

        public struct ForState
        {
            private int posAfterFor;
            private string forVariable;
            private double _from;
            private double _to;
            private double _step;

            public ForState(int PositionAfterFor, string ForVariable, double from, double to, double step)
            {
                this.posAfterFor = PositionAfterFor;
                this.forVariable = ForVariable;
                this._from = from;
                this._to = to;
                this._step = step;
            }

            public int PositionAfterFor { get { return posAfterFor; } set { posAfterFor = value; } }
            public string ForVariable { get { return forVariable; } set { forVariable = value; } }
            public double From { get { return _from; } set { _from = value; } }
            public double To { get { return _to; } set { _to = value; } }
            public double Step { get { return _step; } set { _step = value; } }
        }

        const int MAX_forStack_DEPTH = 4;
        readonly ForState[] forStack = new ForState[MAX_forStack_DEPTH];
        static int forStackPointer;

        // lines

        private struct LineIndex
        {
            private readonly int lineNumber;
            private readonly int programTextPosition;

            public LineIndex(int line_number, int program_text_position)
            {
                this.lineNumber = line_number;
                this.programTextPosition = program_text_position;
            }

            public int LineNumber { get { return lineNumber; } }
            public int ProgramTextPosition { get { return programTextPosition; } }
        }

        readonly List<LineIndex> lidx;

        // Data

        const int MAX_DATA = 10;
        readonly Queue data = new Queue(MAX_DATA);
        int dataPos = 0;

        bool ended;

        private readonly char[] program;

        private readonly Tokenizer tokenizer;
        private readonly Evaluator evaluator;

        int nested = 0;
        int currentLineNumber = 0;

        #endregion
        #region Constructors

        public Interpreter(char[] program, IConsoleIO consoleIO)
        {
            this.consoleIO = consoleIO;        
            lidx = new List<LineIndex>();
            tokenizer = new Tokenizer(program);
            evaluator = new Evaluator(tokenizer);
            this.program = program;
        }

        #endregion Contructors
        #region Methods

        public void Init(int pos)
        {
            Trace.TraceInformation("In Init()");
            program_ptr = pos;
            forStackPointer = 0;
            gosubStackPtr = 0;
            IndexFree();
            tokenizer.Init(pos);
            ended = false;
            Trace.TraceInformation("Out Init()");
        }

        private void IndexFree()
        {
            Trace.TraceInformation("In IndexFree()");
            lidx.Clear();
            Trace.TraceInformation("Out IndexFree()");
        }

        private int IndexFind(int linenum)
        {
            Trace.TraceInformation("In IndexFind()");
            int line = 0;
            LineIndex idx = lidx.Find(x => x.LineNumber == linenum);
            if (idx.LineNumber == 0)
            {
                Debug("IndexFind: Returning zero for " + linenum);
                line = 0;
            }
            else
            {
                Debug("IndexFind: Returning index for line " + Convert.ToString(linenum));
                line = idx.ProgramTextPosition;
            }
            Trace.TraceInformation("Out IndexFind()");
            return (line);
        }

        private void IndexAdd(int linenum, int sourcepos)
        {
            Trace.TraceInformation("In IndexAdd()");
            LineIndex idx = new LineIndex(linenum, sourcepos);
            lidx.Add(idx);
            Debug("IndexAdd: Adding index for line " + Convert.ToString(linenum) + " @ " + Convert.ToString(sourcepos));
            Trace.TraceInformation("Out IndexAdd()");
        }

        private void JumpLineNumberSlow(int linenum)
        {
            Trace.TraceInformation("In JumpLineNumberSlow()");
            tokenizer.Init(program_ptr);

            while (tokenizer.GetInteger() != linenum)
            {
                do
                {
                    do
                    {
                        //tokenizer.tokenizer_next();
                        tokenizer.SkipTokens();
                    }
                    while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR && tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT);

                    if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_CR)
                    {
                        tokenizer.NextToken();
                    }

                }
                while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_INTEGER);
                Debug("JumpLineNumber_slow: Found line " + tokenizer.GetInteger());
            }
            Trace.TraceInformation("Out JumpLineNumberSlow()");
        }

        /// <summary>
        /// Jump to a specific line number
        /// </summary>
        /// <param name="linenum"></param>
        private void JumpLineNumber(int linenum)
        {
            Trace.TraceInformation("In JumpLineNumber()");
            int pos = IndexFind(linenum);
            if (pos > 0)
            {
                Debug("JumpLineNumber: Going to line " + linenum);
                tokenizer.GotoPosition(pos);
            }
            else
            {
                // We'll try to find a yet-unindexed line to jump to.
                Debug("JumpLineNumber: Calling JumpLineNumber_slow " + linenum);
                JumpLineNumberSlow(linenum);
            }
            currentLineNumber = linenum;
            Trace.TraceInformation("Out JumpLineNumber()");
        }


        private bool ReadData(int pos)
        {
            Trace.TraceInformation("In ReadData()");
            bool read = false;
            int current_pos = tokenizer.GetPosition();
            bool negative = false;

            tokenizer.Init(pos);
            do
            {
                do
                {
                    tokenizer.NextToken();
                }
                while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_DATA) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT));
                Debug("ReadData: Read");
				Info("READ");

                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_DATA)
                {
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_DATA);
                    // now read all the data

                    do
                    {
                        double number;
                        if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                        {
                            negative = false;
                            tokenizer.NextToken();
                        }
                        else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMBER)
                        {
                            number = tokenizer.GetNumber();
                            if (negative == true)
                            {
                                Debug("ReadData: add number " + -number);
                                data.Enqueue(-number);
                                negative = false;
                                read = true;
                            }
                            else
                            {
                                Debug("ReadData: add number " + number);
                                data.Enqueue(number);
                                read = true;
                            }
                            tokenizer.NextToken();
                        }
                        else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_INTEGER)
                        {
                            number = tokenizer.GetNumber();
                            if (negative == true)
                            {
                                Debug("ReadData: add integer " + -number);
                                data.Enqueue(-number);
                                negative = false;
                                read = true;
                            }
                            else
                            {
                                Debug("ReadData: add integer " + number);
                                data.Enqueue(number);
                                read = true;
                            }
                            tokenizer.NextToken();
                        }
                        else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING)
                        {
                            Debug("ReadData: add string " + tokenizer.Getstring());
                            data.Enqueue(tokenizer.Getstring());
                            read = true;
                            tokenizer.NextToken();
                        }
                        else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_MINUS)
                        {
                            negative = true;
                            tokenizer.NextToken();
                        }
                        else
                        {
                            negative = false;
                            tokenizer.NextToken();
                        }
                    }
                    while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) && (tokenizer.IsFinished() == false));

                    dataPos = tokenizer.GetPosition();
                    Trace.TraceInformation("In ReadData()");
                }
            }
            while ((read == false) && (tokenizer.IsFinished() == false));
            tokenizer.Init(current_pos);
            Debug("ReadData");
            return (read);
        }

        #region Statements

        // <program> ::= <line> [ <line> ]*
        // <line> ::= <line_number> <statement> [ <statement_separator> <statement> ]* <eol>
        // <line_number> ::= "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "0"
        // <statement_separator> ::= ":"
        // <eol> ::= <cr> <lf> | <cr>

        // <statement> ::= <DataStatement> | <DefStatement> | <IfStatement> | <LetStatement> | <DimStatement>
        //
        // <DataStatement>
        // <IfStatement> ::= "IF" <relation> "THEN"
        // <relation> ::= <expression> <relop> <expression>
        // <relop> ::= "<" | ">" | "=" | "<=" | ">="
        //
        // <LetStatement> ::= "LET" <variable> "=" <expression> [ <separator> <variable> "=" <expression> ]*
        // <variable> ::= <string_variable> | <numeric_variable> | <array_variable>
        // <string_variable> ::= <alpha> [<digit>] "$"
        // <numeric_variable> ::= <alpha> [<digit>]
        // <array_viarable> ::= <alpha> [<digit>] "(" <expression> [ <separator> <expression> ]* ")" 
        //
        // <DimStatement> ::= "DIM" <array_variable> [ <separator> <array_variable> ]*
        //
        // <PrintStatement> ::= "PRINT" <string_variable> | <numeric_variable> | <expresion> | "TAB" "(" <expression> ")" | "

        // <expression> ::= <term> [ <addop> <term> ]*
        // <addop> ::= "+" | "-"
        // <term> ::= <factor> [ <mulop> <factor> ]*
        // <mulop> := "*" | "/"
        // <factor> ::= <number> | <string> | <variable> | <function> | "(" <expression> ")"
        // <function> ::= <name> "(" <expression> [ <separator> <expression> ]* ")"
        // <separator> ::= ","
        // <string> ::= <quote> <alpha> [ <alpha> ]* <quote>
        // <quote> ::= """ | "'"

        /// <summary>
        /// LineStatement
        /// </summary>
        private void LineStatement()
        {
            Debug("LineStatement");
            currentLineNumber = tokenizer.GetInteger();
            Info("----------- Line number " + currentLineNumber + " ---------");
            IndexAdd(currentLineNumber, tokenizer.GetPosition());
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_INTEGER);
            Statement();
            Debug("LineStatement");
        }

        /// <summary>
        /// Statement
        /// </summary>
        private void Statement()
        {
            bool inline = false;
            nested++;
            Tokenizer.Token token;

            Debug("Statement");

            // Might need to consider a loop here for multilne statements
            // otherwise it will error saying the line number is missing.

            do
            {
                token = tokenizer.GetToken();

                switch (token)
                {
                    case Tokenizer.Token.TOKENIZER_DATA:
                        {
                            DataStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_PRINT:
                        {
                            PrintStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_IF:
                        {
                            inline = IfStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_GOTO:
                        {
                            GotoStatement();
                            inline = false;
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_GOSUB:
                        {
                            GosubStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_RETURN:
                        {
                            ReturnStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_FOR:
                        {
                            ForStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_NEXT:
                        {
                            NextStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_END:
                        {
                            EndStatement();
                            break;
                        }
					case Tokenizer.Token.TOKENIZER_STOP:
                        {
                            StopStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_LET:
                        {
                            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LET);
                            LetStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_REM:
                        {
                            RemStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_DIM:
                        {
                            DimStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_READ:
                        {
                            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_READ);
                            ReadStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_DEF:
                        {
                            DefStatement();
                            break;
                        }
                    default:
                        {
                            Abort("statement: Not implemented " + token);
                            break;
                        }
                }

                Debug("statement: " + (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COLON) + " " + (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_CR) + " " + inline);

                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COLON)
                {
                    tokenizer.NextToken();
                    inline = true;
                }
                else if ((tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_CR) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_ENDOFINPUT))
                {
                    Debug("statement: nested=" + nested);
                    if (nested == 1)
                    {
                        tokenizer.NextToken();
                    }
                    inline = false;
                }
            }
            while ((inline == true) && (tokenizer.IsFinished() == false));
            nested--;

            Debug("Statement");

        }

        private void DataStatement()
        {
            Debug("DataStatement");

            tokenizer.SkipTokens();

            Debug("dat_statement");
        }

        /// <summary>
        /// GOTO
        /// </summary>
        private void GotoStatement()
        {
            Debug("GotoStatement");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_GOTO);
            int lineNumber = tokenizer.GetInteger();
            Info("GOTO " + lineNumber);
            JumpLineNumber(lineNumber);
            Debug("GotoStatement");
        }

        /// <summary>
        /// PRINT
        /// </summary>
        private void PrintStatement()
        {
            Debug("PrintStatement");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_PRINT);
            // Print
            // PRINT "HELLO"{CR}
            // PRINT "A=";A{CR}
            // PRINT "HELLO":LET A=5{CR}
            // PRINT "A=";A:LET A=5{CR}
            // PRINT TAB(10);"A=";A{CR}

            char control;
            do
            {
                Debug("PrintStatement: Print loop");
                int tab;
                double number;
                string value;
                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING)
                {
                    Emit(tokenizer.Getstring());
                    tokenizer.NextToken();
                    control = '\n';
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                {
                    // assume a tab spacing of 15 characters
                    // spec defines 5 zones then new line
                    tab = -consoleIO.Hpos + ZONE_WIDTH * (1 + (consoleIO.Hpos / ZONE_WIDTH));
                    Emit(new string(' ', tab));
                    control = ',';
                    tokenizer.NextToken();
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_SEMICOLON)
                {
                    // assume a tab spacing of 3 characters
                    // spec defines a minimum of 6 characters (ignore at the moment)
                    tab = -consoleIO.Hpos + COMPACT_WIDTH * (1 + (consoleIO.Hpos / COMPACT_WIDTH));
                    if (tab < 2)
                    {
                        tab += 3;
                    }
                    Emit(new string(' ', tab));
                    control = ';';
                    tokenizer.NextToken();
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COLON)
                {
                    control = '\n';
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_CR)
                {
                    control = '\n';
                }
                else if ((tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_INTERGER_VARIABLE) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_INTEGER) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE))
                {
                    evaluator.Expression();
                    number = evaluator.PopDouble();
                    value = FormatNumber(number);
                    Emit(value);
                    control = '\n';
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_VARIABLE)
                {
                    evaluator.Expression();
                    Emit(evaluator.PopString());
                    control = '\n';
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_TAB)
                {
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_TAB);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                    evaluator.Expression();
                    tab = (int)Math.Truncate(evaluator.PopDouble()) - consoleIO.Hpos;
                    Emit(new string(' ', tab));
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    control = '\n';
                }
                else
                {
                    evaluator.Expression();
                    number = evaluator.PopDouble();
                    value = FormatNumber(number);
                    Emit(value);
                    control = '\n';
                }
                Debug("PrintStatement: " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON));
            }
            while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON));

            if (control == '\n')
            {
                Emit("\n");
            }

            Debug("PrintStatement");
        }

        /// <summary>
        /// IF
        /// </summary>
        /// <returns></returns>
        private bool IfStatement()
        {
            // The if statement 
            // IF A=1 THEN B=3{CR}
            // IF A=1 PRINT "a=";a{CR}
            // IF A=1 THEN 20{CR}
            // IF A=1 THEN B=3{COLON}LET C=4
            // IF A=1 PRINT "a=";a{COLON}GOTO 20{CR}

            bool jump = true;
            Debug("IfStatement");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_IF);
            Info("IF");
            evaluator.Relation();
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_THEN);
            if (evaluator.PopBoolean())
            {
                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_INTEGER)
                {
                    int lineNumber = tokenizer.GetInteger();
                    JumpLineNumber(lineNumber);
                    //accept(Tokenizer.Token.TOKENIZER_NUMBER);
                    jump = false;
                }
                else if ((tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_INTERGER_VARIABLE) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_VARIABLE))
                {
                    evaluator.Expression();
                }
                else
                {
                    Statement();
                }
            }
            else
            {
                do
                {
                    tokenizer.NextToken();
                }
                while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ELSE) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT));
                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_ELSE)
                {
                    tokenizer.NextToken();
                    Statement();
                }
                //else if(tokenizer.tokenizer_token() == Tokenizer.Token.TOKENIZER_CR)
                //{
                //    tokenizer.tokenizer_next();
                //}
            }
            Debug("IfStatement");
            return (jump);
        }

        /// <summary>
        /// LET
        /// </summary>
        private void LetStatement()
        {
            string varName;
            int integer;
            double number;
            string value;
            int dimensions = 0;
            int[] dimension = new int[10]; // 10 dimensional array limit !!!

            // The let statement can loop through a series of comma separated values and finish at a terminator
            // LET A=1{COMMA}B=2{CR}
            // LET A=1{COMMA}B=2:

            Debug("LetStatement");

            do
            {
                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE)
                {
                    varName = tokenizer.GetNumericVariable();
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_EQ);
                    evaluator.Expression();
                    number = evaluator.PopDouble();
                    evaluator.SetNumericVariable(varName, number);
                    Debug("LetStatement: assign " + number + " to numeric variable " + Convert.ToString(varName));
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_VARIABLE)
                {
                    varName = tokenizer.GetStringVariable();
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STRING_VARIABLE);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_EQ);
                    evaluator.Expression();
                    value = evaluator.PopString();
                    evaluator.SetStringVariable(varName, value);
                    Debug("LetStatement: assign " + value + " to string variable " + Convert.ToString(varName) + "$");
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE)
                {
                    varName = tokenizer.GetNumericArrayVariable();
                    dimension[0] = 0;
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE);
                    do
                    {
                        if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                        {
                            tokenizer.NextToken();
                        }
                        else
                        {
                            evaluator.Expression();
                            integer = (int)Math.Truncate(evaluator.PopDouble());
                            dimensions++;
                            dimension[dimensions] = integer;
                        }
                    }
                    while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_EQ);
                    evaluator.Expression();
                    number = evaluator.PopDouble();
                    evaluator.SetNumericArrayVariable(varName, dimensions, dimension, number);
                    Debug("LetStatement: assign " + number + " to array variable " + Convert.ToString(varName) + "(");
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                {
                    tokenizer.NextToken();
                }
                Debug("LetStatement: " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) + " " + (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));
            }
            while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));
            Debug("LetStatement");
        }

        /// <summary>
        /// DIM
        /// </summary>
        private void DimStatement()
        {
            string varName = "";
            int numeric;
            int dimensions = 0;
            int[] dimension = new int[10]; // 10 dimensional array limit !!!

            // The dim statement can loop through a series of comma separated declarations
            // DIM A(1,1),B(1,2)....{CR}

            Debug("DimStatement");

            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_DIM);
            do
            {
                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE)
                {
                    varName = tokenizer.GetNumericVariable();
                    dimensions = 0;
                    dimension[0] = 0;
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE);
                    do
                    {
                        if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                        {
                            tokenizer.NextToken();
                        }
                        else
                        {
                            evaluator.Expression();
                            numeric = (int)Math.Truncate(evaluator.PopDouble());
                            dimensions++;
                            dimension[dimensions] = numeric;
                        }
                    }
                    while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    evaluator.DeclareNumericArrayVariable(varName, dimensions, dimension);
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                {
                    tokenizer.NextToken();
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_CR)
                {
                    // Skip
                }
                Debug("DimStatement: declare array variable " + varName + " as " + Convert.ToString(dimensions) + " dimensional");

            }
            while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));

            Debug("DimStatement");
        }

        /// <summary>
        /// DEF
        /// </summary>
        private void DefStatement()
        {
            int parameters = 0;
            string[] parameter = new string[10]; // 10 parameter array limit !!!

            // The def statement is followed by they function reference and then an expression
            // DEF FN{function}({parameters})={expresion}

            Debug("DefStatement");

            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_DEF);
            if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_FN)
            {
                tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_FN);
                string varName = tokenizer.GetNumericArrayVariable();
                int num = varName[0] - (int)'a';
                tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE);
                do
                {
                    if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                    {
                        tokenizer.NextToken();
                    }
                    else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE)
                    {
                        // this is the variable name
                        varName = tokenizer.GetNumericVariable();
                        parameter[parameters] = varName;
                        parameters++;
                        tokenizer.NextToken();
                    }
                    else
                    {
                        tokenizer.NextToken();
                    }
                }
                while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);

                tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_EQ);
                evaluator.functions[num] = new Evaluator.function_index(tokenizer.GetPosition(), parameters, parameter);
                tokenizer.SkipTokens();
            }
            Debug("DefStatement");
        }

        /// <summary>
        /// GOSUB
        /// </summary>
        private void GosubStatement()
        {
            int linenum;

            Debug("GosubStatement");

            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_GOSUB);
            linenum = tokenizer.GetInteger();
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_INTEGER);
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_CR);  // this is probematic
            if (gosubStackPtr < MAX_GOSUB_STACK_DEPTH)
            {
                gosubStack[gosubStackPtr] = tokenizer.GetInteger();
                gosubStackPtr++;
                JumpLineNumber(linenum);
            }
            else
            {
                Abort("GosubStatement: gosub stack exhausted");
            }
            Debug("GosubStatement");
        }

        /// <summary>
        /// RETURN
        /// </summary>
        private void ReturnStatement()
        {
            Debug("ReturnStatement");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RETURN);
            if (gosubStackPtr > 0)
            {
                gosubStackPtr--;
                JumpLineNumber(gosubStack[gosubStackPtr]);
            }
            else
            {
                Abort("ReturnStatement: non-matching return");
            }
            Debug("ReturnStatement");
        }

        /// <summary>
        /// REM
        /// </summary>
        private void RemStatement()
        {
            Debug("RemStatement");
            tokenizer.SkipTokens();
            Debug("RemStatement");
        }

        /// <summary>
        /// NEXT
        /// </summary>
        private void NextStatement()
        {
            double var;
            Debug("NextStatement Enter");

            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NEXT);
            string varName = tokenizer.GetNumericVariable();
            var = evaluator.PopDouble();
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE);

            Debug("NextStatement variable=" + varName + " value=" + var);

            if (forStackPointer > 0 && varName == forStack[forStackPointer - 1].ForVariable)
            {
                // allow for negative steps
                double step = forStack[forStackPointer - 1].Step;
                evaluator.SetNumericVariable(varName, evaluator.GetNumericVariable(varName) + step);
                if (step > 0)
                {
                    if (evaluator.GetNumericVariable(varName) <= forStack[forStackPointer - 1].To)
                    {
                        tokenizer.GotoPosition(forStack[forStackPointer - 1].PositionAfterFor);
                    }
                    else
                    {
                        forStackPointer--;
                    }
                }
                else
                {
                    if (evaluator.GetNumericVariable(varName) >= forStack[forStackPointer - 1].To)
                    {
                        tokenizer.GotoPosition(forStack[forStackPointer - 1].PositionAfterFor);
                    }
                    else
                    {
                        forStackPointer--;
                    }
                }
            }
            else
            {
                Err("non-matching next (expected " + forStack[forStackPointer - 1].ForVariable + ", found " + Convert.ToString(var) + ")\n");
            }

            Debug("NextStatement Exit");
        }

        /// <summary>
        /// FOR
        /// </summary>
        private void ForStatement()
        {
            double step = 1;

            Debug("ForStatement");

            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_FOR);
            string varName = tokenizer.GetNumericVariable();
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE);
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_EQ);
            evaluator.Expression();
            double from = evaluator.PopDouble();
            evaluator.SetNumericVariable(varName, from);
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_TO);
            evaluator.Expression();
            double to = evaluator.PopDouble();

            if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STEP)
            {
                tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STEP);
                evaluator.Expression();
                step = evaluator.PopDouble();
            }

            if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_CR)
            {
                if (forStackPointer < MAX_forStack_DEPTH)
                {
                    forStack[forStackPointer].PositionAfterFor = tokenizer.GetPosition();
                    forStack[forStackPointer].ForVariable = varName;
                    forStack[forStackPointer].To = to;
                    forStack[forStackPointer].Step = step;
                    Debug("ForStatement: for variable=" + varName + " from=" + from + " to=" + to + " step=" + step);
                    forStackPointer++;
                }
                else
                {
                    Err("ForStatement: for stack depth exceeded");
                }
            }
            else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COLON)
            {
                if (forStackPointer < MAX_forStack_DEPTH)
                {
                    forStack[forStackPointer].PositionAfterFor = tokenizer.GetPosition();
                    forStack[forStackPointer].ForVariable = varName;
                    forStack[forStackPointer].To = to;
                    forStack[forStackPointer].Step = step;
                    Debug("ForStatement: for variable=" + varName + " from=" + from + " to=" + to + " step=" + step);
                    Info("FOR " + Convert.ToString(varName) + "=" + from + " TO " + to + " STEP " + step);
                    forStackPointer++;
                }
                else
                {
                    Err("ForStatement: for stack depth exceeded");
                }
            }
            Debug("ForStatement");
        }

        /// <summary>
        /// END
        /// </summary>
        private void EndStatement()
        {
            Debug("EndStatement");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_END);
            Debug("EndStatement:");
            Info("END");
            ended = true;
            Debug("EndStatement");
        }

        /// <summary>
        /// STOP
        /// </summary>
        private void StopStatement()
        {
            Debug("StopStatement");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STOP);
            Debug("StopStatement:");
            Info("STOP");
            ended = true;
            Debug("StopStatement");
        }

        /// <summary>
        /// READ
        /// </summary>
        private void ReadStatement()
        {
            string varName;
            int integer;
            double number;
            string value;
            int dimensions = 0;
            int[] dimension = new int[10]; // 10 dimensional array limit !!!

            // The read statement can loop through a series of comma separated variables and finish at a terminator
            // READ A{COMMA}B{CR}
            // READ A{COMMA}B{COLON}

            Debug("ReadStatement");

            do
            {
                bool read;
                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE)
                {
                    varName = tokenizer.GetNumericVariable();
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE);

                    // Now need to read the data from the data statement

                    if (data.Count == 0)
                    {
                        read = ReadData(dataPos);
                        if (read == false)
                        {
                            ended = true;
                            Abort("out of data");
                        }
                        else
                        {
                            number = Convert.ToDouble(data.Dequeue());
                            evaluator.SetNumericVariable(varName, number);
                            Debug("ReadStatement: assign " + number + " to numeric variable " + Convert.ToString(varName));
                            Info("READ " + Convert.ToString(varName) + "=" + number);
                        }
                    }
                    else
                    {
                        number = Convert.ToDouble(data.Dequeue());
                        evaluator.SetNumericVariable(varName, number);
                        Debug("ReadStatement: assign " + number + " to numeric variable " + Convert.ToString(varName));
                        Info("READ " + Convert.ToString(varName) + "=" + number);
                    }
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_VARIABLE)
                {
                    varName = tokenizer.GetStringVariable();
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STRING_VARIABLE);

                    // Now need to read the data from the data statement

                    if (data.Count == 0)
                    {
                        read = ReadData(dataPos);
                        if (read == false)
                        {
                            ended = true;
                            Abort("out of data");
                        }
                        else
                        {
                            value = Convert.ToString(data.Dequeue());
                            evaluator.SetStringVariable(varName, value);
                            Debug("ReadStatement: assign " + value + "to string variable " + Convert.ToString(varName) + "$");
                            Info("READ " + Convert.ToString(varName) + "$=" + value);
                        }
                    }
                    else
                    {
                        value = Convert.ToString(data.Dequeue());
                        evaluator.SetStringVariable(varName, value);
                        Debug("ReadStatement: assign " + value + "to string variable " + Convert.ToString(varName) + "$");
                        Info("READ " + Convert.ToString(varName) + "$=" + value);
                    }
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE)
                {
                    if (data.Count == 0)
                    {
                        read = ReadData(dataPos);
                        if (read == false)
                        {
                            ended = true;
                            Abort("out of data");
                        }
                    }

                    varName = tokenizer.GetNumericArrayVariable();
                    dimension[0] = 1;
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE);
                    do
                    {
                        if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                        {
                            tokenizer.NextToken();
                        }
                        else
                        {
                            evaluator.Expression();
                            integer = (int)Math.Truncate(evaluator.PopDouble());
                            dimensions++;
                            dimension[dimensions] = integer;
                        }
                    }
                    while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);

                    number = Convert.ToDouble(data.Dequeue());
                    evaluator.SetNumericArrayVariable(varName, dimensions, dimension, number);
                    Debug("ReadStatement: assign " + number + " to array variable " + Convert.ToString(varName) + "(");
                    Info("READ " + Convert.ToString(varName) + "()=" + number);

                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                {
                    tokenizer.NextToken();
                }
                Debug("ReadStatement: " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) + " " + (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));
            }
            while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));
            Debug("ReadStatement");
        }

        #endregion Statements

        private string FormatNumber(Double number)
        {
            char sign = ' ';

            Debug("FormatNumber");

            // a number may contain upto 9 digits excuding the decimal point, and 1 digit for sign (+ve is space)
            // , so 11 total
            // it appears that the leading zero of a number is removed 
            //
            // need to format the numeric string
            // {sign}{integer} <= 11 digits
            // {sign}{integer}{.}{decimal} <= 11 digits
            // {sign}{integer}{.}{decimal}{E}{sign}{exponent}
            //

            string value;
            // check if +/- integer

            if (Math.Truncate(number) == number)
            {
                value = Convert.ToString(number);
                if (value.Substring(0, 1) == "-")
                {
                    sign = '-';
                    value = value.Substring(1);     // remove the sign
                }
                if (value.Length > 9)
                {
                    value = Math.Abs(number).ToString("0.######E0");
                }
                value = sign + value;
            }
            else
            {
                value = Convert.ToString(number);
                if (value.Substring(0, 1) == "-")
                {
                    sign = '-';
                }
                if (Math.Abs(number) < 0.1)
                {
                    value = Math.Abs(number).ToString("0.######E0");
                }
                else
                {
                    value = Math.Abs(number).ToString("0.######");
                }

                if (value.Substring(0, 1) == "0")
                {
                    value = value.Substring(1);     // remove the leading zero
                }

                value = sign + value;
            }
            Debug("FormatNumber");
            return (value);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// UbasicRun()
        /// </summary>
        public void Run()
        {
            Debug("Run");
            if (tokenizer.IsFinished())
            {
                Debug("Program finished");
                return;
            }
            LineStatement();
            Debug("Run");
        }

        public bool Finished()
        {
            Debug("Finished");
            bool finished = ended || tokenizer.IsFinished();
            Debug("Finished");
            return (finished);
        }

        //--------------------------------------------------------------
        // Report an Error

        private void Err(string s)
        {
            string message = s + " @ " + currentLineNumber;
            consoleIO.Error(message + "\n");
            if (log.IsErrorEnabled == true) { log.Error(message); }
        }

        //--------------------------------------------------------------
        // Report Error and Halt

        public void Abort(string s)
        {
            string message = s + " @ " + currentLineNumber;
            throw new Exception(message);
        }

        //--------------------------------------------------------------
        // Debug

        private void Debug(string s)
        {
            log.Debug(s);
        }

        //--------------------------------------------------------------
        // Info

        void Info(string s)
        {
            log.Info(s);
        }

        private void Emit(string s)
        {
            consoleIO.Out(s);
        }

        #endregion Methods

    }
}