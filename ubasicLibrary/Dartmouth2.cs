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

using System;
using log4net;
using System.Collections.Generic;
using System.Collections;

namespace ubasicLibrary
{
    /// <summary>
    /// Dartmouth basic version 2 - Oct 68 
    /// </summary>
    public class Dartmouth2 : IBasic
    {
        #region Variables

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        IConsoleIO consoleIO;

        int program_ptr;
        const int MAX_STRINGLEN=40;

        // Printing

        const int ZONE_WIDTH = 15;
        const int COMPACT_WIDTH = 3;

        // Gosub

        const int MAX_GOSUB_STACK_DEPTH = 10;
        int[] gosub_stack = new int[MAX_GOSUB_STACK_DEPTH];
        int gosub_stack_ptr;

        // for-next

        private struct for_state
        {
            private int posAfterFor;
            private string forVariable;
            private double _to;
            private double _step;

            public for_state(int pos_after_for, string for_variable, double to, double step)
            {
                this.posAfterFor = pos_after_for;
                this.forVariable = for_variable;
                this._to = to;
                this._step = step;
            }

            public int pos_after_for { get { return posAfterFor; } set { posAfterFor = value; } }
            public string for_variable { get { return forVariable; } set { forVariable = value; } }
            public double to {get{return _to;} set{_to = value;}}
            public double step { get { return _step; } set { _step = value; } }
        }
    
        const int MAX_FOR_STACK_DEPTH = 4;
        for_state[] for_stack = new for_state[MAX_FOR_STACK_DEPTH];
        static int for_stack_ptr;

        // lines

        private struct line_index
        {
            private int lineNumber;
            private int programTextPosition;

            public line_index(int line_number, int program_text_position)
            {
                this.lineNumber = line_number;
                this.programTextPosition = program_text_position;
            }

            public int line_number { get { return lineNumber; } }
            public int program_text_position { get { return programTextPosition; } }
        }

        List<line_index> lidx;

        // Data

        const int MAX_DATA = 10;
        Queue data = new Queue(MAX_DATA);
        int dataPos = 0;

        bool ended;

        private char[] program;

        private Tokenizer tokenizer;
        private Evaluation evaluation;

        
        int nested = 0;

        int current_line_number = 0;

        #endregion

        #region Constructors

        public Dartmouth2(char[] program, IConsoleIO consoleIO)
        {
            this.consoleIO = consoleIO;        
            lidx = new List<line_index>();
            tokenizer = new Tokenizer(program);
            evaluation = new Evaluation(tokenizer);
            this.program = program;
        }

        #endregion Contructors

        #region Methods

        public void Init(int pos)
        {
            Debug("Init: Enter");
            program_ptr = pos;
            for_stack_ptr = 0;
            gosub_stack_ptr = 0;
            index_free();
            tokenizer.Init(pos);
            ended = false;
            Debug("Init: Exit");
        }

        private void index_free()
        {
            lidx.Clear();
        }

        private int index_find(int linenum)
        {
            int line = 0;
            Debug("index_find: Enter");
            line_index idx = lidx.Find(x => x.line_number == linenum);
            if (idx.line_number == 0)
            {
                Debug("index_find: Returning zero for " + linenum);
                line = 0;
            }
            else
            {
                Debug("index_find: Returning index for line " + Convert.ToString(linenum));
                line = idx.program_text_position;
            }
            Debug("index_find: Exit");
            return (line);
        }

        private void index_add(int linenum, int sourcepos)
        {
            Debug("index_add: Enter");
            line_index idx = new line_index(linenum, sourcepos);
            lidx.Add(idx);
            Debug("index_add: Adding index for line " + Convert.ToString(linenum) + " @ " + Convert.ToString(sourcepos));
            Debug("index_add: Exit");
        }

        private void jump_linenum_slow(int linenum)
        {
            Debug("jump_linenumber_slow: Enter");
            tokenizer.Init(program_ptr);

            while(tokenizer.GetInteger() != linenum)
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
                Debug("jump_linenum_slow: Found line " + tokenizer.GetInteger());
            }
            Debug("jump_linenumber_slow: Exit");
        }

        private void jump_linenum(int linenum)
        {
            Debug("jump_linenum: Enter");
            int pos = index_find(linenum);
            if (pos > 0)
            {
                Debug("jump_linenum: Going to line " + linenum);
                tokenizer.GotoPosition(pos);
            }
            else
            {
                // We'll try to find a yet-unindexed line to jump to.
                Debug("jump_linenum: Calling jump_linenum_slow " + linenum);
                jump_linenum_slow(linenum);
            }
            current_line_number = linenum;
            Debug("jump_linenumber: Exit");
        }


        private bool read_data(int pos)
        {
            bool read = false;
            int current_pos = tokenizer.GetPosition();
            Double number= 0;
            bool negative = false;

            Debug("read_data: Enter");
            tokenizer.Init(pos);
            do
            {
                do
                {
                    tokenizer.NextToken();
                }
                while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_DATA) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT));

                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_DATA)
                {
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_DATA);
                    // now read all the data

                    do
                    {
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
                                Debug("read_data: add number " + -number);
                                data.Enqueue(-number);
                                negative = false;
                                read = true;
                            }
                            else
                            {
                                Debug("read_data: add number " + number);
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
                                Debug("read_data: add integer " + -number);
                                data.Enqueue(-number);
                                negative = false;
                                read = true;
                            }
                            else
                            {
                                Debug("read_data: add integer " + number);
                                data.Enqueue(number);
                                read = true;
                            }
                            tokenizer.NextToken();
                        }
                        else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING)
                        {
                            Debug("read_data: add string " + tokenizer.Getstring());
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
                    while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON));

                    dataPos = tokenizer.GetPosition();
                    Debug("read_data: Found data");
                }
            }
            while ((read == false) && (tokenizer.IsFinished() == false));
            tokenizer.Init(current_pos);
            Debug("read_data: Exit");
            return (read);
        }

        #region Statements

        // <program> ::= <line> [ <line> ]*
        // <line> ::= <line_number> <statement> [ <statement_separator> <statement> ]* <eol>
        // <line_number> ::= "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "0"
        // <statement_separator> ::= ":"
        // <eol> ::= <cr> <lf> | <cr>

        // <statement> ::= <data_statement> | <def_statement> | <if_statement> | <let_statement> | <dim_statement>
        //
        // <if_statement> ::= "IF" <relation> "THEN"
        // <relation> ::= <expression> <relop> <expression>
        // <relop> ::= "<" | ">" | "=" | "<=" | ">="
        //
        // <let_statement> ::= "LET" <variable> "=" <expression> [ <separator> <variable> "=" <expression> ]*
        // <variable> ::= <string_variable> | <numeric_variable> | <array_variable>
        // <string_variable> ::= <alpha> [<digit>] "$"
        // <numeric_variable> ::= <alpha> [<digit>]
        // <array_viarable> ::= <alpha> [<digit>] "(" <expression> [ <separator> <expression> ]* ")" 
        //
        // <dim_statement> ::= "DIM" <array_variable> [ <separator> <array_variable> ]*
        //
        // <print_statement> ::= "PRINT" <string_variable> | <numeric_variable> | <expresion> | "TAB" "(" <expression> ")" | "

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
        /// Line_Statement
        /// </summary>
        private void line_statement()
        {
            Debug("line_statement: Enter");
            current_line_number = tokenizer.GetInteger();
            Debug("----------- Line number " + current_line_number + " ---------");
            index_add(current_line_number, tokenizer.GetPosition());
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_INTEGER);
            Statement();
            Debug("line_statement: Exit");
        }

        private void Statement()
        {
            bool inline = false;
            nested = nested + 1;
            Tokenizer.Token token;

            Debug("Statement: Enter");

            // Might need to consider a loop here for multilne statements
            // otherwise it will error saying the line number is missing.

            do
            {
                token = tokenizer.GetToken();

                switch (token)
                {
                    case Tokenizer.Token.TOKENIZER_DATA:
                        {
                            data_statement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_PRINT:
                        {
                            print_statement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_IF:
                        {
                            inline = if_statement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_GOTO:
                        {
                            goto_statement();
                            inline = false;
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_GOSUB:
                        {
                            gosub_statement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_RETURN:
                        {
                            return_statement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_FOR:
                        {
                            for_statement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_NEXT:
                        {
                            next_statement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_END:
                        {
                            end_statement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_STOP:
                        {
                            stop_statement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_LET:
                        {
                            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LET);
                            let_statement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_REM:
                        {
                            rem_statement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_DIM:
                        {
                            dim_statement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_READ:
                        {
                            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_READ);
                            read_statement();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_DEF:
                        { 
                            def_statement();
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
            while (inline == true);
            nested = nested - 1;

            Debug("statement: Exit");

        }

        private void data_statement()
        {
            Debug("data_statement: Enter");

            tokenizer.SkipTokens();

            Debug("dat_statement: Exit");
        }

        private void goto_statement()
        {
            int lineNumber = 0;

            Debug("goto_statement: Enter");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_GOTO);
            lineNumber = tokenizer.GetInteger();
            jump_linenum(lineNumber);
            Debug("goto_statement: Exit");
        }

        private void print_statement()
        {
            // Print
            // PRINT "HELLO"{CR}
            // PRINT "A=";A{CR}
            // PRINT "HELLO":LET A=5{CR}
            // PRINT "A=";A:LET A=5{CR}
            // PRINT TAB(10);"A=";A{CR}

            char control = '\n';
            int tab = 0;
            double number = 0;
            string value = ""; 

            Debug("print_statement: Enter");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_PRINT);
            do
            {
                Debug("print_statement: Print loop");
                if(tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING)
                {
                    Emit(tokenizer.Getstring());
                    tokenizer.NextToken();
                    control = '\n';
                }
                else if(tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                {
                    // assume a tab spacing of 15 characters
                    // spec defines 5 zones then new line
                    tab = -consoleIO.Hpos + ZONE_WIDTH * (1 + (consoleIO.Hpos / ZONE_WIDTH));
                    Emit(new string(' ', tab));
                    control = ',';
                    tokenizer.NextToken();
                }
                else if(tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_SEMICOLON)
                {
                    // assume a tab spacing of 3 characters
                    // spec defines a minimum of 6 characters (ignore at the moment)
                    tab = -consoleIO.Hpos + COMPACT_WIDTH * (1 + (consoleIO.Hpos / COMPACT_WIDTH));
                    if (tab < 2)
                    {
                       tab = tab + 3;
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
                else if((tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_INTERGER_VARIABLE) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_INTEGER) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE)) 
                {
                    evaluation.Expression();
                    number = evaluation.PopDouble();
                    value = FormatNumber(number);
                    Emit(value);
                    control = '\n';
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_VARIABLE)
                {
                    evaluation.Expression();
                    Emit(evaluation.PopString());
                    control = '\n';
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_TAB)
                {
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_TAB);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                    evaluation.Expression();
                    tab = (int)Math.Truncate(evaluation.PopDouble()) - consoleIO.Hpos;
                    Emit(new string(' ', tab));
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    control = '\n';
                }
                else
                {
                    evaluation.Expression();
                    number = evaluation.PopDouble();
                    value = FormatNumber(number);
                    Emit(value);
                    control = '\n';
                }
                Debug("print_statement: " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON));
            }
            while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON));

            if (control == '\n')
            {
                Emit("\n");
            }

            Debug("print_statement: Exit");
        }

        private bool if_statement()
        {
            // The if statement 
            // IF A=1 THEN B=3{CR}
            // IF A=1 PRINT "a=";a{CR}
            // IF A=1 THEN 20{CR}
            // IF A=1 THEN B=3:LET C=4
            // IF A=1 PRINT "a=";a:GOTO 20{CR}

            bool jump = true;
            int lineNumber = 0;

            Debug("if_statement: Enter");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_IF);
            evaluation.Relation();
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_THEN);
            if(evaluation.PopBoolean())
            {
                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_INTEGER)
                {
                    lineNumber = tokenizer.GetInteger();
                    jump_linenum(lineNumber);
                    //accept(Tokenizer.Token.TOKENIZER_NUMBER);
                    jump = false;
                }
                else if ((tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE) || (tokenizer.GetToken()== Tokenizer.Token.TOKENIZER_INTERGER_VARIABLE) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_VARIABLE))
                {
                    evaluation.Expression();
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
                while((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ELSE) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT));
                if(tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_ELSE)
                {
                    tokenizer.NextToken();
                    Statement();
                }
                //else if(tokenizer.tokenizer_token() == Tokenizer.Token.TOKENIZER_CR)
                //{
                //    tokenizer.tokenizer_next();
                //}
            }
            Debug("if_statement: Exit");
            return (jump);
        }

        private void let_statement()
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

            Debug("let_statement: Enter");

            do
            {
                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE)
                {
                    varName = tokenizer.GetNumericVariable();
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_EQ);
                    evaluation.Expression();
                    number = evaluation.PopDouble();
                    evaluation.SetNumericVariable(varName, number);
                    Debug("let_statement: assign " + number + " to numeric variable " + Convert.ToString(varName));
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_VARIABLE)
                {
                    varName = tokenizer.GetStringVariable();
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STRING_VARIABLE);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_EQ);
                    evaluation.Expression();
                    value = evaluation.PopString();
                    evaluation.SetStringVariable(varName, value);
                    Debug("let_statement: assign " + value + " to string variable " + Convert.ToString(varName) + "$");
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
                            evaluation.Expression();
                            integer = (int)Math.Truncate(evaluation.PopDouble());
                            dimensions = dimensions + 1;
                            dimension[dimensions] = integer;
                        }
                    }
                    while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_EQ);
                    evaluation.Expression();
                    number = evaluation.PopDouble();
                    evaluation.SetNumericArrayVariable(varName,dimensions, dimension,number);
                    Debug("let_statement: assign " + number + " to array variable " + Convert.ToString(varName) + "(");
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                {
                    tokenizer.NextToken();
                }
                Debug("let_statement: " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) + " " + (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));
            }
            while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));
            Debug("let_statement: Exit");
        }

         private void dim_statement()
        {
            string varName = "";
            int numeric;
            int dimensions = 0;
            int[] dimension = new int[10]; // 10 dimensional array limit !!!

            // The dim statement can loop through a series of comma separated declarations
            // DIM A(1,1),B(1,2)....{CR}

            Debug("dim_statement: Enter");

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
                            evaluation.Expression();
                            numeric = (int)Math.Truncate(evaluation.PopDouble());
                            dimensions = dimensions + 1;
                            dimension[dimensions] = numeric;
                        }
                    }
                    while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    evaluation.DeclareNumericArrayVariable(varName, dimensions, dimension);
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                {
                    tokenizer.NextToken();
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_CR)
                {
                    // Skip
                }
                Debug("dim_statement: declare array variable " + varName + " as " + Convert.ToString(dimensions) + " dimensional");
                     
            }
            while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));

            Debug("dim_statement: Exit");
        }

        private void def_statement()
        {
            string varName = "";
            int num = 0;
            int parameters = 0;
            string[] parameter = new string[10]; // 10 parameter array limit !!!

            // The def statement is followed by they function reference and then an expression
            // DEF FN{function}({parameters})={expresion}

            Debug("def_statement: Enter");

            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_DEF);
            if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_FN)
            {
                tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_FN);
                varName = tokenizer.GetNumericArrayVariable();
                num = varName[0] - (int)'a';
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
                        parameters = parameters + 1;
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
                evaluation.functions[num] = new Evaluation.function_index(tokenizer.GetPosition(),parameters,parameter);
                tokenizer.SkipTokens();                    
            }    
            Debug("def_statement: Exit");
        }

        private void gosub_statement()
        {
            int linenum;

            Debug("gosub_statement: Enter");

            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_GOSUB);
            linenum = tokenizer.GetInteger();
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_INTEGER);
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_CR);  // this is probematic
            if(gosub_stack_ptr < MAX_GOSUB_STACK_DEPTH)
            {
                gosub_stack[gosub_stack_ptr] = tokenizer.GetInteger();
                gosub_stack_ptr++;
                jump_linenum(linenum);
            }
            else
            {
                Abort("gosub_statement: gosub stack exhausted");
            }
            Debug("gosub_statement: Exit");
        }
    
        private void return_statement()
        {
            Debug("return_statement: Enter");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RETURN);
            if (gosub_stack_ptr > 0)
            {
                gosub_stack_ptr--;
                jump_linenum(gosub_stack[gosub_stack_ptr]);
            }
            else
            {
                Abort("return_statement: non-matching return");
            }
            Debug("return_statement: Exit");
        }

        private void rem_statement()
        {
            Debug("rem_statement: Enter");
            tokenizer.SkipTokens();
            Debug("rem_statement: Exit");
        }

        private void next_statement()
        {
            double var;
            string varName = "";
            double step = 1;

            Debug("next_statement: Enter");

            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NEXT);
            varName = tokenizer.GetNumericVariable();
            var = evaluation.PopDouble();
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE);

            Debug("next_statement: variable=" + varName + " value=" + var);

            if (for_stack_ptr > 0 && varName == for_stack[for_stack_ptr - 1].for_variable)
            {
                // allow for negative steps
                step = for_stack[for_stack_ptr - 1].step;
                evaluation.SetNumericVariable(varName, evaluation.GetNumericVariable(varName) + step);
                if (step > 0)
                {
                    if (evaluation.GetNumericVariable(varName) <= for_stack[for_stack_ptr - 1].to)
                    {
                        tokenizer.GotoPosition(for_stack[for_stack_ptr - 1].pos_after_for);
                    }
                    else
                    {
                        for_stack_ptr--;
                    }
                }
                else
                {
                    if (evaluation.GetNumericVariable(varName) >= for_stack[for_stack_ptr - 1].to)
                    {
                        tokenizer.GotoPosition(for_stack[for_stack_ptr - 1].pos_after_for);
                    }
                    else
                    {
                        for_stack_ptr--;
                    }
                }
            }
            else
            {
                Err("non-matching next (expected " + for_stack[for_stack_ptr - 1].for_variable + ", found " + Convert.ToString(var) +")\n");
            }

            Debug("next_statement: Exit");
        }

        private void for_statement()
        {
            string varName = "";
            double to = 0;
            double from = 0;
            double step = 1;

            Debug("for_statement: Enter");

            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_FOR);
            varName = tokenizer.GetNumericVariable();
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE);
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_EQ);
            evaluation.Expression();
            from = evaluation.PopDouble();
            evaluation.SetNumericVariable(varName, from);
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_TO);
            evaluation.Expression();
            to = evaluation.PopDouble();

            if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STEP)
            {
                tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STEP);
                evaluation.Expression();
                step = evaluation.PopDouble();
            }

            if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_CR)
            {
                if (for_stack_ptr < MAX_FOR_STACK_DEPTH)
                {
                    for_stack[for_stack_ptr].pos_after_for = tokenizer.GetPosition();
                    for_stack[for_stack_ptr].for_variable = varName;
                    for_stack[for_stack_ptr].to = to;
                    for_stack[for_stack_ptr].step = step;
                    Debug("for_statement: for variable=" + varName + " from=" + from + " to=" + to + " step=" + step);
                    for_stack_ptr++;
                }
                else
                {
                    Err("for_statement: for stack depth exceeded");
                }
            }
            else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COLON)
            {
                if (for_stack_ptr < MAX_FOR_STACK_DEPTH)
                {
                    for_stack[for_stack_ptr].pos_after_for = tokenizer.GetPosition();
                    for_stack[for_stack_ptr].for_variable = varName;
                    for_stack[for_stack_ptr].to = to;
                    for_stack[for_stack_ptr].step = step;
                    Debug("for_statement: for variable=" + varName + " from=" + from + " to=" + to + " step=" + step);
                    for_stack_ptr++;
                }
                else
                {
                    Err("for_statement: for stack depth exceeded");
                }
            }
            Debug("for_statement: Exit");
        }

        private void end_statement()
        {
            Debug("end_statement: Enter");
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_END);
            ended = true;
            Debug("end_statement: Exit");
        }

        private void stop_statement()
        {
            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STOP);
            ended = true;
        }

        private void read_statement()
        {
            string varName;
            int integer;
            double number;
            string value;
            bool read = false;
            int dimensions = 0;
            int[] dimension = new int[10]; // 10 dimensional array limit !!!

            // The read statement can loop through a series of comma separated variables and finish at a terminator
            // READ A{COMMA}B{CR}
            // READ A{COMMA}B{COLON}

            Debug("read_statement: Enter");

            do
            {
                if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE)
                {
                    varName = tokenizer.GetNumericVariable();
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE);

                    // Now need to read the data from the data statement

                    if (data.Count == 0)
                    {
                        read = read_data(dataPos);
                        if (read == false)
                        {
                            ended = true;
                            //Abort("out of data");
                        }
                        else
                        {
                            number = Convert.ToDouble(data.Dequeue());
                            evaluation.SetNumericVariable(varName, number);
                            Debug("read_statement: assign " + number + " to numeric variable " + Convert.ToString(varName));
                        }
                    }
                    else
                    {
                        number = Convert.ToDouble(data.Dequeue());
                        evaluation.SetNumericVariable(varName, number);
                        Debug("read_statement: assign " + number + " to numeric variable " + Convert.ToString(varName));
                    }
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_STRING_VARIABLE)
                {
                    varName = tokenizer.GetStringVariable();
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STRING_VARIABLE);

                    // Now need to read the data from the data statement

                    if (data.Count == 0)
                    {
                        read = read_data(dataPos);
                        if (read == false)
                        {
                            ended = true;
                            //Abort("out of data");
                        }
                        else
                        {
                            value = Convert.ToString(data.Dequeue());
                            evaluation.SetStringVariable(varName, value);
                            Debug("read_statement: assign " + value + "to string variable " + Convert.ToString(varName) + "$");
                        }
                    }
                    else
                    {
                        value = Convert.ToString(data.Dequeue());
                        evaluation.SetStringVariable(varName, value);
                        Debug("read_statement: assign " + value + "to string variable " + Convert.ToString(varName) + "$");
                    }
                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE)
                {
                    if (data.Count == 0)
                    {
                        read = read_data(dataPos);
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
                            evaluation.Expression();
                            integer = (int)Math.Truncate(evaluation.PopDouble());
                            dimensions = dimensions + 1;
                            dimension[dimensions] = integer;
                        }
                    }
                    while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                    
                    number = Convert.ToDouble(data.Dequeue());
                    evaluation.SetNumericArrayVariable(varName, dimensions, dimension, number);
                    Debug("read_statement: assign " + number + " to array variable " + Convert.ToString(varName) + "(");

                }
                else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                {
                    tokenizer.NextToken();
                }
                Debug("read_statement: " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) + " " + (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) + " " + (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));
            }
            while ((tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_CR) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_ENDOFINPUT) && (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_COLON) || (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA));
            Debug("read_statement: Exit");
        }

        #endregion Statements

        private string FormatNumber(Double number)
        {
            // a number may contain upto 9 digits excuding the decimal point, and 1 digit for sign (+ve is space)
            // , so 11 total
            // it appears that the leading zero of a number is removed 
            //
            // need to format the numeric string
            // {sign}{integer} <= 11 digits
            // {sign}{integer}{.}{decimal} <= 11 digits
            // {sign}{integer}{.}{decimal}{E}{sign}{exponent}
            //

            string value = "";
            char sign = ' ';

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
                return (value);
            }
            else
            {
                value = Convert.ToString(number);
                if (value.Substring(0,1) == "-")
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
                return (value);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// UbasicRun()
        /// </summary>
        public void Run()
        {
            if (tokenizer.IsFinished())
            {
                Debug("ubasic program finished");
                return;
            }
            line_statement();
        }

        public bool Finished()
        {
            return (ended || tokenizer.IsFinished());
        }

        //--------------------------------------------------------------
        // Report an Error

        private void Err(string s)
        {
            consoleIO.Error("Error: " + s);
            if (log.IsErrorEnabled == true) { log.Error(s); }
        }

        //--------------------------------------------------------------
        // Report Error and Halt

        public void Abort(string s)
        {
            Err(s);
            throw new Exception(s);
        }

        //--------------------------------------------------------------
        // Debug

        private void Debug(string s)
        {
            if (log.IsDebugEnabled == true) { log.Debug(s); }
        }

        private void Emit(string s)
        {
            consoleIO.Out(s);
        }

        #endregion Methods

    }
}