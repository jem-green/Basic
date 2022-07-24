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

namespace Dartmouth5
{
    /// <summary>
    /// Dartmouth basic version 4 - Oct 68 
    /// </summary>
    public class Interpreter : IInterpreter
    {
        #region Fields

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        readonly IuBasicIO consoleIO;

        int program_ptr;
        const int MAX_STRINGLEN = 40;

        // Gosub

        const int MAX_GOSUB_STACK_DEPTH = 10;
        readonly int[] gosubStack = new int[MAX_GOSUB_STACK_DEPTH];
        int gosubStackPointer;

        // for-next

        public struct ForState
        {
            private int positionAfterFor;
            private string forVariable;
            private double from;
            private double to;
            private double step;

            public ForState(int positionAfterFor, string forVariable, double from, double to, double step)
            {
                this.positionAfterFor = positionAfterFor;
                this.forVariable = forVariable;
                this.from = from;
                this.to = to;
                this.step = step;
            }

            public int PositionAfterFor { get { return positionAfterFor; } set { positionAfterFor = value; } }
            public string ForVariable { get { return forVariable; } set { forVariable = value; } }
            public double From { get { return from; } set { from = value; } }
            public double To { get { return to; } set { to = value; } }
            public double Step { get { return step; } set { step = value; } }
        }

        const int MAX_FOR_STACK_DEPTH = 4;
        readonly ForState[] forStack = new ForState[MAX_FOR_STACK_DEPTH];
        static int forStackPointer;

        // lines

        private struct LineIndex
        {
            private readonly int lineNumber;
            private readonly int programTextPosition;

            public LineIndex(int lineNumber, int programTextPosition)
            {
                this.lineNumber = lineNumber;
                this.programTextPosition = programTextPosition;
            }

            public int LineNumber { get { return lineNumber; } }
            public int ProgramTextPosition { get { return programTextPosition; } }
        }

        readonly List<LineIndex> lineIndex;

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

        public Interpreter(char[] program, IuBasicIO consoleIO)
        {
            this.consoleIO = consoleIO;        
            lineIndex = new List<LineIndex>();
            tokenizer = new Tokenizer(program);
            evaluator = new Evaluator(tokenizer);
            this.program = program;
        }

        #endregion Contructors
        #region Methods

        public void Init(int position)
        {
            Trace.TraceInformation("In Init()");
            program_ptr = position;
            forStackPointer = 0;
            gosubStackPointer = 0;
            IndexFree();
            tokenizer.Init(position);
            ended = false;
            Trace.TraceInformation("Out Init()");
        }

        private void IndexFree()
        {
            Trace.TraceInformation("In IndexFree()");
            lineIndex.Clear();
            Trace.TraceInformation("Out IndexFree()");
        }

        private int IndexFind(int lineNumber)
        {
            Trace.TraceInformation("In IndexFind()");
            int line = 0;
            LineIndex idx = lineIndex.Find(x => x.LineNumber == lineNumber);
            if (idx.LineNumber == 0)
            {
                Debug("IndexFind: Returning zero for " + lineNumber);
                line = 0;
            }
            else
            {
                Debug("IndexFind: Returning index for line " + Convert.ToString(lineNumber));
                line = idx.ProgramTextPosition;
            }
            Trace.TraceInformation("Out IndexFind()");
            return (line);
        }

        private void IndexAdd(int lineNumber, int sourcePosition)
        {
            Trace.TraceInformation("In IndexAdd()");
            LineIndex idx = new LineIndex(lineNumber, sourcePosition);
            lineIndex.Add(idx);
            Debug("IndexAdd: Adding index for line " + Convert.ToString(lineNumber) + " @ " + Convert.ToString(sourcePosition));
            Trace.TraceInformation("Out IndexAdd()");
        }

        private void JumpLineNumberSlow(int lineNumber)
        {
            Trace.TraceInformation("In JumpLineNumberSlow()");
            tokenizer.Init(program_ptr);

            while (tokenizer.GetInteger() != lineNumber)
            {
                do
                {
                    do
                    {
                        //Tokenizer_next();
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
        /// <param name="lineNumber"></param>
        private void JumpLineNumber(int lineNumber)
        {
            Trace.TraceInformation("In JumpLineNumber()");
            int pos = IndexFind(lineNumber);
            if (pos > 0)
            {
                Debug("JumpLineNumber: Going to line " + lineNumber);
                tokenizer.GotoPosition(pos);
            }
            else
            {
                // We'll try to find a yet-unindexed line to jump to.
                Debug("JumpLineNumber: Calling JumpLineNumber_slow " + lineNumber);
                JumpLineNumberSlow(lineNumber);
            }
            currentLineNumber = lineNumber;
            Trace.TraceInformation("Out JumpLineNumber()");
        }

        private string ReadInput()
        {
            Trace.TraceInformation("In ReadInput()");
            string value = consoleIO.In();
            value = value.TrimEnd('\r');
            value = value.TrimEnd('\n');
            Trace.TraceInformation("In ReadInput()");
            return (value);
        }

        private bool ReadData(int position)
        {
            Trace.TraceInformation("In ReadData()");
            bool read = false;
            int current_pos = tokenizer.GetPosition();
            bool negative = false;

            tokenizer.Init(position);
            do
            {
                do
                {
                    tokenizer.NextToken();
                    if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_REM)
                    {
                        RemStatement();
                    }
                }
                while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_DATA) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) && (tokenizer.IsFinished() == false));

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
                    Debug("ReadData: Found data");
                }
            }
            while ((read == false) && (tokenizer.IsFinished() == false));
            tokenizer.Init(current_pos);
            Trace.TraceInformation("Out ReadData()");
            return (read);
        }

        #region Statements

        // <program> ::= <line> [ <line> ]*
        // <line> ::= <line_number> <statement> [ <statement_separator> <statement> ]* <eol>
        // <line_number> ::= <number> [ <zero> | <number> ]*;
        // <number> ::= "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9";
        // <zero> ::= "0";
        // <statement_separator> ::= ":"
        // <eol> ::= <cr> <lf> | <cr>;

        // <statement> ::= <OnStatement> | <IfStatement>
        // <OnStatement> ::= "ON" <expression> "THEN" <line> [ "," <line> ]*
        // <OnStatement> ::= "ON" <expression> "GOTO" <line> [ "," <line> ]*
        // <IfStatement> ::= "IF" <expression><relation><expression> "THEN" <line>
        // <IfStatement> ::= "IF" <expression><relation><expression> "GOTO" <line>
        //
        // <InputStatement> ::= "INPUT" 
        //
        // <function> ::= <name> "(" <expression> [ <separator> <expression> ]* ")"
        // <name> ::= "TAB"

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
            Trace.TraceInformation("In LineStatement()");
            currentLineNumber = tokenizer.GetInteger();
            Info("----------- Line number " + currentLineNumber + " ---------");
            IndexAdd(currentLineNumber, tokenizer.GetPosition());
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_INTEGER);
            Statement();
            Trace.TraceInformation("Out LineStatement()");
        }

        /// <summary>
        /// Statement
        /// </summary>
        private void Statement()
        {
            bool inline = false;
            nested++;
            Tokenizer.Token token;

            Trace.TraceInformation("In Statement()");

            // Might need to consider a loop here for multilne statements
            // otherwise it will error saying the line number is missing.

            do
            {
                token = tokenizer.GetToken();

                switch (token)
                {
                    case Tokenizer.Token.TOKENIZER_INPUT:
                        {
                            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_INPUT);
                            InputStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_DATA:
                        {
                            DataStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_RESTORE:
                        {
                            RestoreStatement();
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
                    case Tokenizer.Token.TOKENIZER_ON:
                        {
                            OnStatement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_RANDOMIZE:
                        {
                            RandomizeStatement();
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

            Trace.TraceInformation("Out Statement()");

        }

        /// <summary>
        /// RANDOMIZE
        /// </summary>
        private void RandomizeStatement()
        {
            Trace.TraceInformation("In RandomizeStatement()");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RANDOMIZE);
            evaluator.Randomize();
            Trace.TraceInformation("Out RandomizeStatement()");
        }

        /// <summary>
        /// DATA
        /// </summary>
        private void DataStatement()
        {
           Trace.TraceInformation("In In DataStatement()");
            tokenizer.SkipTokens();
           Trace.TraceInformation("Out DataStatement()");
        }

        /// <summary>
        /// RESTORE
        /// </summary>
        private void RestoreStatement()
        {
           Trace.TraceInformation("In RestoreStatement()");
           tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RESTORE);
           dataPos = 0;
           Trace.TraceInformation("Out RestoreStatement()");
        }

        /// <summary>
        /// GOTO
        /// </summary>
        private void GotoStatement()
        {
            Trace.TraceInformation("In GotoStatement()");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_GOTO);
            int lineNumber = tokenizer.GetInteger();
            Info("GOTO " + lineNumber);
            JumpLineNumber(lineNumber);
            Trace.TraceInformation("Out GotoStatement()");
        }

        /// <summary>
        /// PRINT
        /// </summary>
        private void PrintStatement()
        {
            Tokenizer.Token previous = Tokenizer.Token.TOKENIZER_NULL;

            Trace.TraceInformation("In PrintStatement()");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_PRINT);
            // Print
            // PRINT "HELLO"{CR}
            // PRINT "A=";A{CR}
            // PRINT "HELLO":LET A=5{CR}
            // PRINT "A=";A:LET A=5{CR}
            // PRINT TAB(10);"A=";A{CR}

            log.Info("PRINT");

            char control;
            do
            {
                Debug("PrintStatement: Print loop");
                int tab;
                double number;
                string value;
                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING)
                {
                    previous = tokenizer.GetToken();
                    value = tokenizer.Getstring();
                    log.Info("PRINT " + value);
                    Emit(value);
                    tokenizer.NextToken();
                    control = '\n';
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                {
                    // assume a tab spacing of 15 characters
                    // spec defines 5 zones then new line

                    previous = tokenizer.GetToken();
                    tab = -consoleIO.CursorLeft + consoleIO.Zone * (1 + (consoleIO.CursorLeft / consoleIO.Zone));
                    value = new string(' ', tab);
                    log.Info("PRINT ,");
                    Emit(value);
                    control = ',';
                    tokenizer.NextToken();

                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_SEMICOLON)
                {
                    if ((previous == Tokenizer.Token.TOKENIZER_STRING) || (previous == Tokenizer.Token.TOKENIZER_STRING_VARIABLE) || (previous == Tokenizer.Token.TOKENIZER_STRING_ARRAY_VARIABLE))
                    {
                        // additional rule appears to be that if the ';' folows text then it concatinates
                        // if ';' follows a number then it move tab zones.
                    }
                    else
                    {
                        // assume a tab spacing of 3 characters
                        // spec defines a minimum of 6 characters

                        tab = -consoleIO.CursorLeft + consoleIO.Compact * (1 + (consoleIO.CursorLeft / consoleIO.Compact));
                        if (tab < 2)
                        {
                            tab += 3;
                        }
                        tab = 1;
                        value = new string(' ', tab);
                        log.Info("PRINT ;");
                        Emit(value);
                    }
                    previous = tokenizer.GetToken();
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
                else if ((tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMBER) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_INTEGER))
                {
                    previous = tokenizer.GetToken();
                    number = tokenizer.GetNumber();
                    value = FormatNumber(number);
                    log.Info("PRINT " + value);
                    Emit(value);
                    control = '\n';
                }
                else if ((tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_INTERGER_VARIABLE) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE))
                {
                    previous = tokenizer.GetToken();
                    evaluator.Expression();
                    number = evaluator.PopDouble();
                    value = FormatNumber(number);
                    log.Info("PRINT " + value);
                    Emit(value);
                    control = '\n';
                }
                else if ((tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_VARIABLE) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_ARRAY_VARIABLE))
                {
                    previous = tokenizer.GetToken();
                    evaluator.Expression();
                    value = evaluator.PopString();
                    log.Info("PRINT " + value);
                    Emit(value);
                    control = '\n';
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_TAB)
                {
                    previous = tokenizer.GetToken();
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_TAB);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                    evaluator.Expression();
                    tab = (int)Math.Truncate(evaluator.PopDouble()) - consoleIO.CursorLeft;
                    if (tab > 0)
                    {
                        value = new string(' ', tab);
                        log.Info("PRINT TAB(" + tab + ")");
                        Emit(value);
                    }
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    control = '\n';
                }
                else
                {
                    evaluator.Expression();

                    // How do we know what to pop off the stack.

                    object data = evaluator.PopObject();
                    if (data.GetType() == typeof(int))
                    {
                        int integer = Convert.ToInt32(data);
                        value = FormatNumber(integer);
                        log.Info("PRINT " + value);
                        Emit(value);
                    }
                    else if (data.GetType() == typeof(double))
                    {
                        number = Convert.ToDouble(data);
                        value = FormatNumber(number);
                        log.Info("PRINT " + value);
                        Emit(value);
                    }
                    previous = tokenizer.GetToken();
                    control = '\n';
                }

                Debug("PrintStatement: " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON));
            }
            while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) && (tokenizer.IsFinished() == false));

            if (control == '\n')
            {
                Emit("\n");
            }

            Trace.TraceInformation("Out PrintStatement()");
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
            Trace.TraceInformation("In IfStatement()");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_IF);
            Info("IF");
            evaluator.Relation();
            int lineNumber;
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_THEN);
            if (evaluator.PopBoolean() == true)
            {
                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_INTEGER)
                {
			
                    lineNumber = tokenizer.GetInteger();
					Info("THEN " + lineNumber);
                    JumpLineNumber(lineNumber);

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
                while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT));
            }
            Trace.TraceInformation("Out IfStatement()");
            return (jump);
        }

        /// <summary>
        /// ON
        /// </summary>
        private void OnStatement()
        {
            // The on statement
            //
            // <statement> ::= <OnStatement> | <IfStatement>
            //
            // <OnStatement> ::= "ON" <expression> "THEN" <line> [ "," <line> ]*
            // <OnStatement> ::= "ON" <expression> "GOTO" <line> [ "," <line> ]*
            //
            // 

            double number;
            int integer;
            int parameter = 0;
            bool check = true;

            Trace.TraceInformation("In OnStatement()");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_ON);
            evaluator.Expression();
            number = evaluator.PopDouble();
            if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_THEN)
            {
                tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_THEN);
            }
            else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_GOTO)
            {
                tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_GOTO);
            }

            integer = (int)Math.Truncate(number);
            if (integer < 1)
            {
                Abort("Expected: > 1");
            }
            else
            {
                do
                {
                    if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_INTEGER)
                    {
                        int lineNumber = tokenizer.GetInteger();
                        parameter++;
                        if (integer == parameter)
                        {
                            Info("ON " + integer + " GOTO " + lineNumber);
                            JumpLineNumber(lineNumber);
                            check = false;
                        }
                        else
                        {
                            tokenizer.NextToken();
                        }
                    }
                    else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                    {
                        tokenizer.NextToken();
                    }
                    else if ((tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_CR) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_ENDOFINPUT) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COLON))
                    {
                        check = false;
                    }
                }
                while (check == true);
            }
            if (integer > parameter)
            {
                Abort("Expected: < " + parameter);
            }
            Trace.TraceInformation("Out OnStatement()");
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

            // The let statement can loop through a series of comma separated values and finish at a terminator
            // LET A=1{COMMA}B=2{CR}
            // LET A=1{COMMA}B=2:

            Trace.TraceInformation("In LetStatement()");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LET);
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
                    Info("LET " + Convert.ToString(varName) + "=" + number);
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_VARIABLE)
                {
                    varName = tokenizer.GetStringVariable();
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STRING_VARIABLE);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_EQ);
                    evaluator.Expression();
                    value = evaluator.PopString();
                    evaluator.SetStringVariable(varName, value);
                    Debug("LetStatement: assign '" + value + "' to string variable " + Convert.ToString(varName) + "$");
                    Info("LET " + Convert.ToString(varName) + "$=\"" + value + "\"");
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE)
                {
                    varName = tokenizer.GetNumericArrayVariable();
                    int[] dimension = new int[10]; // 10 dimensional array limit !!!
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
                    try
                    {
                        evaluator.SetNumericArrayVariable(varName, dimensions, dimension, number);
                    }
                    catch(Exception e)
                    {
                        Debug(e.ToString());
                    }
                    Debug("LetStatement: assign " + number + " to numeric array variable " + Convert.ToString(varName) + "(");
                    Info("LET " + Convert.ToString(varName) + "()" + "=" + number);
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_ARRAY_VARIABLE)
                {
                    varName = tokenizer.GetNumericArrayVariable();
                    int[] dimension = new int[10]; // 10 dimensional array limit !!!
                    dimension[0] = 0;
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STRING_ARRAY_VARIABLE);
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
                    value = evaluator.PopString();
                    evaluator.SetStringArrayVariable(varName, dimensions, dimension, value);
                    Debug("LetStatement: assign '" + value + "' to string array variable " + Convert.ToString(varName) + "(");
                    Info("LET " + Convert.ToString(varName) + "$()" + "=\"" + value + "\"");
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                {
                    tokenizer.NextToken();
                }
                Debug("LetStatement: " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) + " " + (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));
            }
            while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));
            Trace.TraceInformation("Out LetStatement()");
        }

        /// <summary>
        /// INPUT
        /// </summary>
        private void InputStatement()
        {
            string varName;
            int integer;
            double numeric;
            string value;
            int dimension = 0;
            int[] dimensions = new int[10]; // 10 dimensional array limit !!!
            string buffer = "";
            int buf_pointer = 0;

            // The input statement can loop through a series of comma separated values and finish at a terminator
            // INPUT A{COMMA}B{CR}
            // INPUT A{COMMA}B:
            // the input looks like it can also take a number of comma separated values and split out in 1 entry
            // ? 10,10 will read in the two values and assign to A and B.
            // this means I need an input buffer that can be parsed.

            Trace.TraceInformation("In InputStatement()");

            do
            {
                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE)
                {
                    varName = tokenizer.GetNumericVariable();
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE);
                    if ((buffer == "") || (buf_pointer >= buffer.Length))
                    {
                        Emit("?");
                        buf_pointer = 0;
                        buffer = ReadInput();
                        Emit("\n");
                    }
                    value = "";

                    do
                    {
                        if (buffer[buf_pointer] == ',')
                        {
                            buf_pointer++;
                            break;
                        }
                        else
                        {
                            value += buffer[buf_pointer];
                            buf_pointer++;
                        }
                    }
                    while (buf_pointer < buffer.Length);

                    // conversion could error here when we get to alphanumeric inputs
                    if (double.TryParse(value, out numeric) == false)
                    {
                        Err("Expected: Double");
                    }
                    else
                    {
                        evaluator.SetNumericVariable(varName, numeric);
                    }
                    Debug("InputStatement: assign " + numeric + " to numeric variable " + Convert.ToString(varName));
                    Info("INPUT " + Convert.ToString(varName) + "=" + numeric);
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_VARIABLE)
                {
                    varName = tokenizer.GetStringVariable();
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STRING_VARIABLE);
                    if ((buffer == "") || (buf_pointer >= buffer.Length))
                    {
                        Emit("?");
                        buf_pointer = 0;
                        buffer = ReadInput();
                        Emit("\n");
                    }
                    value = "";

                    do
                    {
                        if (buffer[buf_pointer] == ',')
                        {
                            buf_pointer++;
                            break;
                        }
                        else
                        {
                            value += buffer[buf_pointer];
                            buf_pointer++;
                        }
                    }
                    while (buf_pointer < buffer.Length);

                    evaluator.SetStringVariable(varName, value);
                    Debug("InputStatement: assign " + value + " to string variable " + Convert.ToString(varName) + "$");
                    Info("INPUT " + Convert.ToString(varName) + "$=" + value);
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE)
                {
                    varName = tokenizer.GetNumericArrayVariable();
                    dimensions[0] = 0;
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
                            dimension++;
                            dimensions[dimension] = integer;
                        }
                    }
                    while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);

                    // wonder what the original did here if a number is not entered?
                    // looks like it exits out back to the command shell

                    if ((buffer == "") || (buf_pointer >= buffer.Length))
                    {
                        Emit("?");
                        buf_pointer = 0;
                        buffer = ReadInput();
                        Emit("\n");
                    }
                    value = "";

                    do
                    {
                        if (buffer[buf_pointer] == ',')
                        {
                            buf_pointer++;
                            break;
                        }
                        else
                        {
                            value += buffer[buf_pointer];
                            buf_pointer++;
                        }
                    }
                    while (buf_pointer < buffer.Length);

                    // conversion could error here when we get to alphanumeric inputs
                    if (!double.TryParse(value, out numeric))
                    {
                        Err("Expected: Double");
                    }
                    evaluator.SetNumericArrayVariable(varName, dimension, dimensions, numeric);
                    Debug("InputStatement: assign " + numeric + " to array variable " + Convert.ToString(varName) + "(");
                    Info("INPUT " + Convert.ToString(varName) + "()=" + numeric);
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_ARRAY_VARIABLE)
                {
                    varName = tokenizer.GetNumericArrayVariable();
                    dimensions[0] = 0;
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STRING_ARRAY_VARIABLE);
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
                            dimension++;
                            dimensions[dimension] = integer;
                        }
                    }
                    while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);

                    // wonder what the original did here if a number is not entered?
                    // looks like it exits out back to the command shell

                    if ((buffer == "") || (buf_pointer >= buffer.Length))
                    {
                        Emit("?");
                        buf_pointer = 0;
                        buffer = ReadInput();
                        Emit("\n");
                    }
                    value = "";

                    do
                    {
                        if (buffer[buf_pointer] == ',')
                        {
                            buf_pointer++;
                            break;
                        }
                        else
                        {
                            value += buffer[buf_pointer];
                            buf_pointer++;
                        }
                    }
                    while (buf_pointer < buffer.Length);

                    evaluator.SetStringArrayVariable(varName, dimension, dimensions, value);
                    Debug("InputStatement: assign " + value + " to string array variable " + Convert.ToString(varName) + "(");
                    Info("INPUT " + Convert.ToString(varName) + "()=" + value);
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                {
                    tokenizer.NextToken();
                }
                Debug("InputStatement: " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) + " " + (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));
            }
            while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));
            Trace.TraceInformation("Out InputStatement()");
        }

        /// <summary>
        /// DIM
        /// </summary>
        private void DimStatement()
        {
            int numeric;

            // The dim statement can loop through a series of comma separated declarations
            // DIM A(1,1),B(1,2)....{CR}

            Trace.TraceInformation("In DimStatement()");

            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_DIM);
            do
            {
                string varName;
                int dimensions;
                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE)
                {
                    varName = tokenizer.GetNumericVariable();
                    dimensions = 0;
                    int[] dimension = new int[10]; // 10 dimensional array limit !!!
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
                    Debug("DimStatement: declare string array variable " + varName + " as " + Convert.ToString(dimensions) + " dimensional");
                    Info("DIM " + Convert.ToString(varName) + "$(" + Convert.ToString(dimensions) + ")");
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_ARRAY_VARIABLE)
                {
                    varName = tokenizer.GetNumericVariable();
                    dimensions = 0;
                    int[] dimension = new int[10]; // 10 dimensional array limit !!!
                    dimension[0] = 0;
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STRING_ARRAY_VARIABLE);
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
                    evaluator.DeclareStringArrayVariable(varName, dimensions, dimension);
                    Debug("DimStatement: declare string array variable " + varName + " as " + Convert.ToString(dimensions) + " dimensional");
                    Info("DIM " + Convert.ToString(varName) + "(" + Convert.ToString(dimensions) + ")");
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                {
                    tokenizer.NextToken();
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_CR)
                {
                    // Skip
                }
            }
            while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON)) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));

            Trace.TraceInformation("Out DimStatement()");
        }

        /// <summary>
        /// DEF
        /// </summary>
        private void DefStatement()
        {
            int parameters = 0;
            string[] parameter = new string[10]; // 10 parameter array limit !!!

            // The def statement is followed by the function reference and then an expression
            // DEF FN{function}({parameters})={expresion}

            Trace.TraceInformation("In DefStatement()");

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
                evaluator.functions[num] = new Evaluator.FunctionIndex(tokenizer.GetPosition(), parameters, parameter);
                tokenizer.SkipTokens();
            }
            Trace.TraceInformation("Out DefStatement()");
        }

        /// <summary>
        /// GOSUB
        /// </summary>
        private void GosubStatement()
        {
            int lineNumber;

            Trace.TraceInformation("In GosubStatement()");

            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_GOSUB);
            lineNumber = tokenizer.GetInteger();
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_INTEGER);
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_CR);  // this is probematic
            if (gosubStackPointer < MAX_GOSUB_STACK_DEPTH)
            {
                gosubStack[gosubStackPointer] = tokenizer.GetInteger();
                gosubStackPointer++;
                JumpLineNumber(lineNumber);
            }
            else
            {
                Abort("GosubStatement: gosub stack exhausted");
            }
            Trace.TraceInformation("Out GosubStatement()");
        }

        /// <summary>
        /// RETURN
        /// </summary>
        private void ReturnStatement()
        {
            Trace.TraceInformation("In ReturnStatement()");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RETURN);
            if (gosubStackPointer > 0)
            {
                gosubStackPointer--;
                JumpLineNumber(gosubStack[gosubStackPointer]);
            }
            else
            {
                Abort("ReturnStatement: non-matching return");
            }
            Trace.TraceInformation("Out ReturnStatement()");
        }

        /// <summary>
        /// REM
        /// </summary>
        private void RemStatement()
        {
            Trace.TraceInformation("In RemStatement");
            tokenizer.SkipTokens();
            Trace.TraceInformation("Out RemStatement");
        }

        /// <summary>
        /// NEXT
        /// </summary>
        private void NextStatement()
        {
            double var;
            Trace.TraceInformation("In NextStatement()");

            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NEXT);
            string varName = tokenizer.GetNumericVariable();
            var = evaluator.PopDouble();
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE);

            Debug("NextStatement: variable=" + varName + " value=" + var);
            Info("NEXT " + Convert.ToString(varName));

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

            Trace.TraceInformation("Out NextStatement()");
        }

        /// <summary>
        /// FOR
        /// </summary>
        private void ForStatement()
        {
            double step = 1;

            Trace.TraceInformation("In ForStatement()");

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
                if (forStackPointer < MAX_FOR_STACK_DEPTH)
                {
                    // nasty effect where a goto bypasses the next.
                    // option is to check if the variable already exists and re-sue

                    for (int i=0; i < MAX_FOR_STACK_DEPTH; i++)
                    {
                        if (forStack[i].ForVariable == varName)
                        {
                            forStackPointer = i;
                            break;
                        }
                    }

                    forStack[forStackPointer].PositionAfterFor = tokenizer.GetPosition();
                    forStack[forStackPointer].ForVariable = varName;
                    forStack[forStackPointer].From = from;
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
            else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COLON)
            {
                if (forStackPointer < MAX_FOR_STACK_DEPTH)
                {

                    for (int i = 0; i < MAX_FOR_STACK_DEPTH; i++)
                    {
                        if (forStack[i].ForVariable == varName)
                        {
                            forStackPointer = i;
                            break;
                        }
                    }

                    forStack[forStackPointer].PositionAfterFor = tokenizer.GetPosition();
                    forStack[forStackPointer].ForVariable = varName;
                    forStack[forStackPointer].From = from;
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
            Trace.TraceInformation("Out ForStatement()");
        }

        /// <summary>
        /// END
        /// </summary>
        private void EndStatement()
        {
            Trace.TraceInformation("In EndStatement()");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_END);
            Info("END");
            ended = true;
            Trace.TraceInformation("Out EndStatement()");
        }

        /// <summary>
        /// STOP
        /// </summary>
        private void StopStatement()
        {
            Trace.TraceInformation("In StopStatement()");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STOP);
            Info("STOP");
            ended = true;
            Trace.TraceInformation("Out StopStatement()");
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

            Trace.TraceInformation("In ReadStatement()");

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
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_ARRAY_VARIABLE)
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
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STRING_ARRAY_VARIABLE);
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

                    value = Convert.ToString(data.Dequeue());
                    evaluator.SetStringArrayVariable(varName, dimensions, dimension, value);
                    Debug("ReadStatement: assign " + value + " to array variable " + Convert.ToString(varName) + "(");
                    Info("READ " + Convert.ToString(varName) + "()=" + value);

                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                {
                    tokenizer.NextToken();
                }
                Debug("ReadStatement: " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) + " " + (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));
            }
            while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));
           Trace.TraceInformation("Out ReadStatement()");
        }

        #endregion Statements

        private string FormatNumber(Double number)
        {
            Trace.TraceInformation("In FormatNumber()");
            char sign = ' ';


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
                    value = Math.Abs(number).ToString("0.##### E+0");
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
                if ((Math.Abs(number) < 0.1) && (value.Length > 6))
                {
                    value = Math.Abs(number).ToString("0.##### E+0");
                }
                else
                {
                    if (Math.Abs(number) < 1)
                    {
                        value = Math.Abs(number).ToString(".######");
                    }
                    else
                    {
                        value = Math.Abs(number).ToString("0.#####");
                    }
                }
                value = sign + value;
            }
            Trace.TraceInformation("Out FormatNumber()");
            return (value);
        }

        /// <summary>
        /// Run()
        /// </summary>
        public void Run()
        {
            Trace.TraceInformation("In Run()");
            if (tokenizer.IsFinished())
            {
                Debug("Program finished");
                return;
            }
            LineStatement();
            Trace.TraceInformation("Out Run()");
        }

        /// <summary>
        /// Finished()
        /// </summary>
        /// <returns></returns>
        public bool Finished()
        {
            Trace.TraceInformation("In Finished()");
            bool finished = ended || tokenizer.IsFinished();
            Trace.TraceInformation("Out Finished()");
            return (finished);
        }

        #endregion Methods
        #region Private

        /// <summary>
        /// Error level log to console
        /// </summary>
        /// <param name="text"></param>
        private void Err(string text)
        {
            string message = text + " @ " + currentLineNumber;
            consoleIO.Error(message + "\n");
            if (log.IsErrorEnabled == true) { log.Error(message); }
        }

        /// <summary>
        /// Raise exception
        /// </summary>
        /// <param name="text"></param>
        public void Abort(string text)
        {
            string message = text + " @ " + currentLineNumber;
            throw new Exception(message);
        }

        /// <summary>
        /// Debug level log
        /// </summary>
        /// <param name="text"></param>
        private void Debug(string text)
        {
            log.Debug(text);
        }

        /// <summary>
        /// Info level log
        /// </summary>
        /// <param name="text"></param>
        void Info(string text)
        {
            log.Info(text);
        }

        /// <summary>
        /// Output data
        /// </summary>
        /// <param name="s"></param>
        private void Emit(string s)
        {
            consoleIO.Out(s);
        }

        #endregion
    }
}