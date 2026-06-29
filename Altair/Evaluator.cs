// Copyright (C) 1988 Jack W. Crenshaw. All rights reserved. 

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using BasicLibrary;
using System.Diagnostics;
using TracerLibrary;

namespace Altair
{
    public class Evaluator
    {
        #region Fields

        readonly Stack<object> stack;

        private readonly Tokenizer tokenizer;

        const int MAX_VARNUM = 26;
        readonly int[] variables = new int[MAX_VARNUM];
        readonly Hashtable stringVariables;
        readonly Hashtable numericVariables;
        readonly Hashtable numericArrayVariables;
        readonly Hashtable stringArrayVariables;

        // functions

        public struct FunctionIndex
        {
            private readonly int programTextPosition;
            private readonly int @params;
            private readonly string[] param;

            public FunctionIndex(int pos, int parameters, string[] parameter)
            {
                this.programTextPosition = pos;
                this.@params = parameters;
                this.param = parameter;
            }
            public int ProgramTextPosition { get { return programTextPosition; } }
            public int Parameters { get { return @params; } }
            public string[] Parameter { get { return param; } }

        }
        const int MAX_FUNCTIONS = 26;
        public FunctionIndex[] functions;

        int randomize = 0;
        private readonly IDefaultIO _IO;

        #endregion
        #region Constructors

        public Evaluator(Tokenizer tokenizer, IDefaultIO io)
        {
            stack = new Stack<object>();
            this.tokenizer = tokenizer;
            this._IO = io;
            stringVariables = new Hashtable();
            numericVariables = new Hashtable();
            numericArrayVariables = new Hashtable();
            stringArrayVariables = new Hashtable();
            functions = new FunctionIndex[MAX_FUNCTIONS];
        }

        #endregion Constructors
        #region Properties



        #endregion Properties
        #region Methods

        public void Randomize()
        {
            Debug.WriteLine("In Evaluator.Randomize()");
            randomize = Environment.TickCount;
            Debug.WriteLine("Out Evaluator.Randomize()");
        }

        // <b-expression>  ::= <b-term> [<orop> <b-term>]*
        // <b-term>        ::= <not-factor> [AND <not-factor>]*
        // <not-factor>    ::= [NOT] <b-factor>
        // <b-factor>      ::= <b-literal> | <b-variable> | <relation>
        // <relation>      ::= | <expression> [<relop> <expression]
        // <expression>    ::= <term> [<addop> <term>]*
        // <term>          ::= <signed factor> [<mulop> factor]*
        // <signed factor> ::= [<addop>] <factor>
        // <factor>        ::= <integer> | <variable> | (<b-expression>)


        /// <summary>
        /// BinaryExpression
        /// </summary>
        public void BinaryExpression()
        {
            Tokenizer.Token op;

            Debug.WriteLine("In Evaluator.BinaryExpression()");
            BinaryTerm();

            op = tokenizer.GetToken();
            TraceInternal.TraceVerbose("BinaryExpression: token " + Convert.ToString(op));
            while (op == Tokenizer.Token.TOKENIZER_XOR || op == Tokenizer.Token.TOKENIZER_OR)
            {
                tokenizer.NextToken();
                BinaryTerm();
                switch (op)
                {
                    case Tokenizer.Token.TOKENIZER_XOR:
                        {
                            Xor();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_OR:
                        {
                            Or();
                            break;
                        }
                }
                op = tokenizer.GetToken();
            }

            Debug.WriteLine("Out Evaluator.BinaryExpression()");
        }

        /// <summary>
        /// BinaryTerm
        /// </summary>
        public void BinaryTerm()
        {
            Tokenizer.Token op;

            Debug.WriteLine("In Evaluator.BinaryTerm()");
            BinaryNotFactor();

            op = tokenizer.GetToken();
            TraceInternal.TraceVerbose("BinaryTerm: token " + Convert.ToString(op));
            while (op == Tokenizer.Token.TOKENIZER_AND)
            {
                tokenizer.NextToken();
                BinaryNotFactor();
                switch (op)
                {
                    case Tokenizer.Token.TOKENIZER_AND:
                        {
                            And();
                            break;
                        }
                }
                op = tokenizer.GetToken();
            }

            Debug.WriteLine("Out Evaluator.BinaryTerm()");
        }

        /// <summary>
        /// BinaryNotFactor
        /// </summary>
        /// <returns></returns>
        public void BinaryNotFactor()
        {
            Tokenizer.Token op;

            Debug.WriteLine("In Evaluator.BinaryNotFactor()");
            BinaryFactor();

            op = tokenizer.GetToken();
            TraceInternal.TraceVerbose("BinaryNotFactor: token " + Convert.ToString(op));
            while (op == Tokenizer.Token.TOKENIZER_NOT)
            {
                tokenizer.NextToken();
                BinaryFactor();
                switch (op)
                {
                    case Tokenizer.Token.TOKENIZER_NOT:
                        {
                            Not();
                            break;
                        }
                }
                op = tokenizer.GetToken();
            }

            Debug.WriteLine("Out Evaluator.BinaryNotFactor()");
        }

        public void BinaryFactor()
        {
            Debug.WriteLine("In Evaluator.BinaryFactor()");
            Relation();
            Debug.WriteLine("Out Evaluator.BinaryFactor()");

        }

        /// <summary>
        /// Relation
        /// </summary>
        /// <returns></returns>
        public void Relation()
        {
            Tokenizer.Token op;

            Debug.WriteLine("In Evaluator.Relation()");
            Expression();
            op = tokenizer.GetToken();

            TraceInternal.TraceVerbose("relation: token " + Convert.ToString(op));
            while (op == Tokenizer.Token.TOKENIZER_LT || op == Tokenizer.Token.TOKENIZER_GT || op == Tokenizer.Token.TOKENIZER_EQ)
            {
                tokenizer.NextToken();

                // Check here if the op is a combined <= or <> or >= in this order

                if ((op == Tokenizer.Token.TOKENIZER_LT) && (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_EQ))
                {
                    op = Tokenizer.Token.TOKENIZER_LTEQ;
                    tokenizer.NextToken();
                }
                else if ((op == Tokenizer.Token.TOKENIZER_LT) && (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_GT))
                {
                    op = Tokenizer.Token.TOKENIZER_NOTEQ;
                    tokenizer.NextToken();
                }
                else if ((op == Tokenizer.Token.TOKENIZER_GT) && (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_EQ))
                {
                    op = Tokenizer.Token.TOKENIZER_GTEQ;
                    tokenizer.NextToken();
                }

                Expression();

                switch (op)
                {
                    case Tokenizer.Token.TOKENIZER_LT:
                        {
                            Less();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_GT:
                        {
                            Greater();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_EQ:
                        {
                            Equal();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_LTEQ:
                        {
                            LessEqual();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_NOTEQ:
                        {
                            NotEqual();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_GTEQ:
                        {
                            GreaterEqual();
                            break;
                        }
                }
                op = tokenizer.GetToken();
            }
            Debug.WriteLine("Out Evaluator.Relation()");
        }

        /// <summary>
        /// Expression
        /// </summary>
        public void Expression()
        {
            Tokenizer.Token op;
            Debug.WriteLine("In Evaluator.Expression()");

            // check if negative number

            if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_MINUS)
            {
                tokenizer.NextToken();
                stack.Push((double)0);
                Term();
                Subtract();
            }
            else
            {
                Term();
            }
            op = tokenizer.GetToken();
            TraceInternal.TraceVerbose("Expression: token " + Convert.ToString(op));
            while (op == Tokenizer.Token.TOKENIZER_PLUS || op == Tokenizer.Token.TOKENIZER_MINUS)
            {
                tokenizer.NextToken();
                Term();
                switch (op)
                {
                    case Tokenizer.Token.TOKENIZER_PLUS:
                        {
                            Add();
                            break;
                        }
                    case Tokenizer.Token.TOKENIZER_MINUS:
                        {
                            Subtract();
                            break;
                        }
                }
                op = tokenizer.GetToken();
            }
            Debug.WriteLine("Out Evaluator.Expression()");
        }

        /// <summary>
        /// Term
        /// </summary>
        /// <returns></returns>
        private void Term()
        {
            Debug.WriteLine("In Evaluator.Term()");
            Tokenizer.Token op;

            TraceInternal.TraceVerbose("Term: token " + tokenizer.GetToken());
            Exponent();
            op = tokenizer.GetToken();
            TraceInternal.TraceVerbose("Term: token " + op);

            while (op == Tokenizer.Token.TOKENIZER_ASTR || op == Tokenizer.Token.TOKENIZER_SLASH || op == Tokenizer.Token.TOKENIZER_MOD)
            {
                tokenizer.NextToken();
                TraceInternal.TraceVerbose("Term: token " + tokenizer.GetToken());
                Exponent();

                switch (op)
                {
                    case Tokenizer.Token.TOKENIZER_ASTR:
                        {
                            Multiply();
                            break;
                        };
                    case Tokenizer.Token.TOKENIZER_SLASH:
                        {
                            Divide();
                            break;
                        }
                }
                op = tokenizer.GetToken();
            }
            Debug.WriteLine("Out Evaluator.Term()");
        }

        /// <summary>
        /// Exponent
        /// </summary>
        /// <returns></returns>
        private void Exponent()
        {
            Tokenizer.Token op;
            Debug.WriteLine("In Evaluator.Exponent()");

            TraceInternal.TraceVerbose("Exponent: token " + tokenizer.GetToken());
            switch (tokenizer.GetToken())
            {
                case Tokenizer.Token.TOKENIZER_FUNCTION:
                    {
                        break;
                    }

                default:
                    {
                        Factor();
                        break;
                    }
            }

            op = tokenizer.GetToken();
            TraceInternal.TraceVerbose("Exponent: token " + op);
            while (op == Tokenizer.Token.TOKENIZER_EXPONENT)
            {
                tokenizer.NextToken();
                TraceInternal.TraceVerbose("Exponent: token " + tokenizer.GetToken());
                switch (tokenizer.GetToken())
                {
                    case Tokenizer.Token.TOKENIZER_FUNCTION:
                        {
                            break;
                        }
                    default:
                        {
                            Factor();
                            break;
                        }
                }

                switch (op)
                {
                    case Tokenizer.Token.TOKENIZER_EXPONENT:
                        {
                            Power();
                            break;
                        };
                }
                op = tokenizer.GetToken();
            }
            Debug.WriteLine("Out Evaluator.Exponent()");
        }

        /// <summary>
        /// Factor
        /// </summary>
        private void Factor()
        {
            object f;
            string varName;
            FunctionIndex function;
            int num;

            Debug.WriteLine("In Evaluator.Factor()");

            TraceInternal.TraceVerbose("Factor: token " + tokenizer.GetToken());
            switch (tokenizer.GetToken())
            {
                case Tokenizer.Token.TOKENIZER_FN:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_FN);
                        varName = tokenizer.GetNumericArrayVariable();
                        TraceInternal.TraceVerbose("Factor: function " + varName);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE);
                        num = varName[0] - (int)'a';
                        function = functions[num];

                        // a number of paramerters that could be expressions until the ')'

                        do
                        {
                            if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                            {
                                tokenizer.NextToken();
                            }
                            else if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_CR)
                            {
                                // Skip
                            }
                            else
                            {
                                BinaryExpression();
                                // this will be left the stack in reverse order
                            }
                        }
                        while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);

                        // assign the expressions to the variables in the correct order

                        for (int i = function.Parameters - 1; i >= 0; i--)
                        {
                            f = PopDouble();
                            TraceInternal.TraceVerbose("Factor: function numeric " + Convert.ToString(f));
                            SetNumericVariable(function.Parameter[i], (double)f);
                        }

                        // now jump to the function execute and then restore the position and continue 

                        int current_pos = tokenizer.GetPosition();
                        tokenizer.Init(function.ProgramTextPosition);
                        BinaryExpression();
                        tokenizer.Init(current_pos);
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_ABS:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_ABS);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Abs();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_ATN:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_ATN);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Atn();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_COS:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_COS);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Cos();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_EXP:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_EXP);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Exp();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_INT:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_INT);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Int();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_LOG:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LOG);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Log();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_RND:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RND);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Rnd();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_SGN:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_SGN);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Sgn();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_SIN:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_SIN);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Sin();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_SQR:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_SQR);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        SquareRoot();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_TAN:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_TAN);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Tan();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_LEFT:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFT);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_COMMA);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Left();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_RIGHT:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHT);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_COMMA);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Right();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_MID:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_MID);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_COMMA);
                        BinaryExpression();
                        if (tokenizer.GetToken() == Tokenizer.Token.TOKENIZER_COMMA)
                        {
                            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_COMMA);
                            BinaryExpression();
                            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                            Mid(3);
                        }
                        else
                        {
                            tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                            Mid(2);
                        }
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_ASC:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_ASC);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Asc();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_VAL:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_VAL);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Val();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_CHR:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_CHR);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Chr();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_LEN:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEN);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Len();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_STR:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STR);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Str();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_POS:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_POS);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        stack.Pop();
                        stack.Push((double)_IO.CursorLeft);
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_USR:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_USR);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        throw new NotImplementedException("USR");
                    }
                case Tokenizer.Token.TOKENIZER_FRE:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_FRE);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        throw new NotImplementedException("FRE");
                    }
                case Tokenizer.Token.TOKENIZER_INP:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_INP);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        throw new NotImplementedException("INP");
                    }
                case Tokenizer.Token.TOKENIZER_PEEK:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_PEEK);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        throw new NotImplementedException("PEEK");
                    }
                case Tokenizer.Token.TOKENIZER_NUMBER:
                    {
                        f = tokenizer.GetNumber();
                        TraceInternal.TraceVerbose("Factor: number " + Convert.ToString(f));
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMBER);
                        stack.Push(f);
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_INTEGER:
                    {
                        f = (double)tokenizer.GetInteger();
                        TraceInternal.TraceVerbose("Factor: integer " + Convert.ToString(f));
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_INTEGER);
                        stack.Push(f);
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_STRING:
                    {
                        f = tokenizer.Getstring();
                        TraceInternal.TraceVerbose("Factor: string '" + Convert.ToString(f) + "'");
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STRING);
                        stack.Push((string)f);
                        break;
                    }

                case Tokenizer.Token.TOKENIZER_LEFTPAREN:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        BinaryExpression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_STRING_VARIABLE:
                    {
                        f = GetStringVariable(tokenizer.GetStringVariable());
                        TraceInternal.TraceVerbose("Factor: string variable '" + Convert.ToString(f) + "'");
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STRING_VARIABLE);
                        stack.Push(f);
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE:
                    {
                        f = GetNumericVariable(tokenizer.GetNumericVariable());
                        TraceInternal.TraceVerbose("Factor: numeric variable " + Convert.ToString(f));
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE);
                        stack.Push(f);
                        break;
                    }

                case Tokenizer.Token.TOKENIZER_NUMERIC_ARRAY_VARIABLE:
                    {
                        int numeric;
                        int dimension = 0;
                        int[] dimensions = new int[10];
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
                                BinaryExpression();
                                numeric = (int)Math.Truncate(PopDouble());
                                dimension++;
                                dimensions[dimension] = numeric;
                            }
                        }
                        while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);

                        f = GetNumericArrayVariable(varName, dimension, dimensions);
                        TraceInternal.TraceVerbose("Factor: numeric array " + Convert.ToString(f));
                        stack.Push(f);
                        break;
                    }

                case Tokenizer.Token.TOKENIZER_STRING_ARRAY_VARIABLE:
                    {
                        int numeric;
                        int dimension = 0;
                        int[] dimensions = new int[10];
                        varName = tokenizer.GetStringArrayVariable();

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
                                BinaryExpression();
                                numeric = (int)Math.Truncate(PopDouble());
                                dimension++;
                                dimensions[dimension] = numeric;
                            }
                        }
                        while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);

                        f = GetStringArrayVariable(varName, dimension, dimensions);
                        TraceInternal.TraceVerbose("Factor: string array " + Convert.ToString(f));
                        stack.Push(f);
                        break;
                    }

                default:
                    {
                        num = tokenizer.GetIntegerVariable();
                        f = GetIntVariable(num);
                        TraceInternal.TraceVerbose("Factor: int " + Convert.ToString(f));
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_INTEGER);
                        stack.Push(f);
                        break;
                    }
            }
            Debug.WriteLine("Out Evaluator.Factor()");
        }

        #region functions

        //---------------------------------------------------------------}
        // SQRT Top of Stack with Primary
        private void SquareRoot()
        {
            object first;
            double number;
            Debug.WriteLine("In Evaluator.SquareRoot()");

            if (stack.Count > 0)
            {
                first = stack.Pop();
                if (first.GetType() == typeof(string))
                {
                    // only expecting an integer or double
                    Expected("double");
                }
                else
                {
                    if ((double)first >= 0)
                    {
                        number = Math.Sqrt((double)first);
                        TraceInternal.TraceVerbose("PopSqr: " + number);
                        stack.Push(number);
                    }
                    else
                    {
                        Expected("positive");
                    }
                }
            }
            Debug.WriteLine("Out Evaluator.SquareRoot()");
        }

        //---------------------------------------------------------------}
        // ABS Top of Stack with Primary
        private void Abs()
        {
            // This just removes the ecimal part with no rounding acording to the specification

            object first;
            double number;
            Debug.WriteLine("In Evaluator.Abs()");

            if (stack.Count > 0)
            {
                first = stack.Pop();
                if (first.GetType() == typeof(string))
                {
                    // only expecting an integer or double
                    Expected("double");
                }
                else
                {
                    number = Math.Abs((double)first);
                    TraceInternal.TraceVerbose("Abs: " + number);
                    stack.Push(number);
                }
            }
            Debug.WriteLine("Out Evaluator.Abs()");
        }

        //---------------------------------------------------------------}
        // INT Top of Stack with Primary
        private void Int()
        {
            // This just removes the decimal part with no rounding acording to the specification

            object first;
            double number;
            Debug.WriteLine("In Evaluator.Int()");

            if (stack.Count > 0)
            {
                first = stack.Pop();
                if (first.GetType() == typeof(string))
                {
                    // only expecting an integer or double
                    Expected("double");
                }
                else
                {
                    number = Math.Truncate((double)first);
                    TraceInternal.TraceInformation("INT(\"" + first + "\")");
                    TraceInternal.TraceVerbose("Int: " + number);
                    stack.Push(number);
                }
            }
            Debug.WriteLine("Out Evaluator.Int()");
        }

        //---------------------------------------------------------------}
        // RND Top of Stack with Primary
        private void Rnd()
        {
            object first;
            double number;
            Debug.WriteLine("In Evaluator.Rnd()");

            if (stack.Count > 0)
            {
                first = stack.Pop();
                if (first.GetType() == typeof(string))
                {
                    // only expecting an integer or double
                    Expected("double");
                }
                else
                {
                    // the specification implies that the parameter has no value
                    // and that the same random sequence is always derived when the
                    // program starts
                    //Random r = new Random((int)Math.Truncate((double)first));
                    Random r = new Random(randomize);
                    randomize--;
                    number = r.NextDouble();
                    TraceInternal.TraceVerbose("Rnd: " + number);
                    stack.Push(number);
                }
            }
            Debug.WriteLine("Out Evaluator.Rnd()");
        }

        //---------------------------------------------------------------}
        // SGN Top of Stack with Primary
        private void Sgn()
        {
            object first;
            Debug.WriteLine("In Evaluator.Sgn()");

            if (stack.Count > 0)
            {
                first = stack.Pop();
                if (first.GetType() == typeof(string))
                {
                    // only expecting an integer or double
                    Expected("double");
                }
                else
                {
                    double number = Math.Sign((double)first);                
                    TraceInternal.TraceInformation("SGN(\"" + first + "\")");
                    TraceInternal.TraceVerbose("Sgn: '" + number + "'");
                    stack.Push(number);
                }
            }
            Debug.WriteLine("Out Evaluator.Sgn()");
        }

        //---------------------------------------------------------------}
        // SIN Top of Stack with Primary
        private void Sin()
        {
            object first;
            Debug.WriteLine("In Evaluator.Sin()");

            if (stack.Count > 0)
            {
                first = stack.Pop();
                if (first.GetType() == typeof(string))
                {
                    // only expecting an integer or double
                    Expected("double");
                }
                else
                {
                    stack.Push(Math.Sin((double)first));
                }
            }
            Debug.WriteLine("Out Evaluator.Sin()");
        }

        //---------------------------------------------------------------}
        // COS Top of Stack with Primary
        private void Cos()
        {
            object first;
            Debug.WriteLine("In Evaluator.Cos()");
            if (stack.Count > 0)
            {
                first = stack.Pop();
                if (first.GetType() == typeof(string))
                {
                    // only expecting an integer or double
                    Expected("double");
                }
                else
                {
                    stack.Push(Math.Cos((double)first));
                }
            }
            Debug.WriteLine("Out Evaluator.Cos()");
        }

        //---------------------------------------------------------------}
        // TAN Top of Stack with Primary
        private void Tan()
        {
            object first;
            Debug.WriteLine("In Evaluator.Tan()");

            if (stack.Count > 0)
            {
                first = stack.Pop();
                if (first.GetType() == typeof(string))
                {
                    // only expecting an integer or double
                    Expected("double");
                }
                else
                {
                    stack.Push(Math.Tan((double)first));
                }
            }
            Debug.WriteLine("Out Evaluator.Tan()");
        }

        //---------------------------------------------------------------}
        // ATN Top of Stack with Primary
        private void Atn()
        {
            object first;
            Debug.WriteLine("In Evaluator.Atn()");

            if (stack.Count > 0)
            {
                first = stack.Pop();
                if (first.GetType() == typeof(string))
                {
                    // only expecting an integer or double
                    Expected("double");
                }
                else
                {
                    stack.Push(Math.Atan((double)first));
                }
            }
            Debug.WriteLine("Out Evaluator.Atn()");
        }

        //---------------------------------------------------------------}
        // ATN Top of Stack with Primary
        private void Exp()
        {
            object first;
            Debug.WriteLine("In Evaluator.Exp()");

            if (stack.Count > 0)
            {
                first = stack.Pop();
                if (first.GetType() == typeof(string))
                {
                    // only expecting an integer or double
                    Expected("double");
                }
                else
                {
                    stack.Push(Math.Exp((double)first));
                }
            }
            Debug.WriteLine("Out Evaluator.Exp()");
        }

        //---------------------------------------------------------------}
        // ATN Top of Stack with Primary
        private void Log()
        {
            object first;
            Debug.WriteLine("In Evaluator.Log()");

            if (stack.Count > 0)
            {
                first = stack.Pop();
                if (first.GetType() == typeof(string))
                {
                    // only expecting an integer or double
                    Expected("double");
                }
                else
                {
                    stack.Push(Math.Log((double)first));
                }
            }
            Debug.WriteLine("Out Evaluator.Log()");
        }

        //---------------------------------------------------------------}
        // ASC Top of Stack with Primary
        private void Asc()
        {
            object first;
            double number = 0;
            Debug.WriteLine("In Evaluator.Asc()");

            if (stack.Count > 0)
            {
                first = stack.Pop();
                if (first.GetType() != typeof(string))
                {
                    // only expecting a string
                    Expected("string");
                }
                else
                {
                    string text = Convert.ToString(first);
                    if (text.Length > 0)
                    {
                        byte[] asciiBytes = Encoding.ASCII.GetBytes(text);
                        number = (double)asciiBytes[0];
                    }
                    stack.Push(number);
                }
            }
            Debug.WriteLine("Out Evaluator.Asc()");
        }

        //---------------------------------------------------------------}
        // CHR Top of Stack with Primary
        private void Chr()
        {
            object first;
            string text = "";
            Debug.WriteLine("In Evaluator.Chr()");

            if (stack.Count > 0)
            {
                first = stack.Pop();
                if (first.GetType() != typeof(double))
                {
                    // only expecting an integer or double
                    Expected("double");
                }
                else
                {
                    double number = Convert.ToDouble(first);
                    if ((number >= 0) && (number <= 255))
                    {
                        byte[] asciiBytes = new byte[1];
                        asciiBytes[0] = (byte)number;
                        text = Encoding.ASCII.GetString(asciiBytes);
                    }
                    stack.Push(text);
                }
            }
            Debug.WriteLine("Out Evaluator.Chr()");
        }

        //---------------------------------------------------------------}
        // LEN Top of Stack with Primary
        private void Len()
        {
            object first;
            Debug.WriteLine("In Evaluator.Len()");

            if (stack.Count > 0)
            {
                first = stack.Pop();
                if (first.GetType() != typeof(string))
                {
                    // only expecting an integer or double
                    Expected("string");
                }
                else
                {
                    double number = first.ToString().Length;
                    TraceInternal.TraceInformation("LEN(\"" + first + "\")");
                    TraceInternal.TraceVerbose("Left: '" + number + "'");
                    stack.Push(number);

                }
            }
            Debug.WriteLine("Out Evaluator.Len()");
        }

        //---------------------------------------------------------------}
        // STR$ Top of Stack with Primary
        private void Str()
        {
            object first;
            string value = "";
            Debug.WriteLine("In Evaluator.Str()");

            if (stack.Count > 0)
            {
                first = stack.Pop();
                if (first.GetType() != typeof(string))
                {
                    try
                    {
                        double number = Convert.ToDouble(first);
                        value = Convert.ToString(number);
                    }
                    catch { }
                    stack.Push(value);
                }
                else
                {
                    Expected("double");
                }
            }
            Debug.WriteLine("Out Evaluator.Str()");
        }

        //---------------------------------------------------------------}
        // VAL Top of Stack with Primary
        private void Val()
        {
            object first;
            double number = 0;
            Debug.WriteLine("In Evaluator.Val()");

            if (stack.Count > 0)
            {
                first = stack.Pop();
                if (first.GetType() != typeof(string))
                {
                    Expected("string");
                }
                else
                { 
                    try
                    {
                        string value = Convert.ToString(first);
                        number = Convert.ToDouble(value);

                    }
                    catch { }
                    stack.Push(number);
                }
            }
            Debug.WriteLine("Out Evaluator.Val()");
        }

        //---------------------------------------------------------------}
        // LEFT$ Top of Stack with Primary
        // 1 - length -> first
        // 0 - string -> second

        private void Left()
        {
            object first;
            object second;
            string value;
            int length;

            Debug.WriteLine("In Evaluator.Left()");

            if (stack.Count > 1)
            {
                first = stack.Pop();
                if (first.GetType() != typeof(string))
                {
                    if (stack.Count > 0)
                    {
                        second = stack.Pop();
                        if (second.GetType() == typeof(string))
                        {
                            length = (int)Math.Truncate(Convert.ToDouble(first));
                            value = second.ToString();
                            if (length < 1)
                            {
                                value = "";
                                TraceInternal.TraceVerbose("Left: '" + value + "'");
                                stack.Push(value);
                            }
                            else if (length >= value.Length)
                            {
                                TraceInternal.TraceVerbose("Left: '" + value + "'");
                                stack.Push(value);
                            }
                            else
                            {
                                value = value.Substring(0, length);
                                TraceInternal.TraceInformation("LEFT(\"" + value + "\"," + length + ")");
                                TraceInternal.TraceVerbose("Left: '" + value + "'");
                                stack.Push(value);
                            }
                        }
                        else
                        {
                            Expected("string");
                        }
                    }
                }
                else
                {
                    Expected("double");
                }
            }
            Debug.WriteLine("Out Evaluator.Left()");
        }

        //---------------------------------------------------------------}
        // RIGHT$ Top of Stack with Primary
        // 1 - length -> first
        // 0 - string -> second

        private void Right()
        {
            object first;
            object second;
            string value;
            int length;
            Debug.WriteLine("In Evaluator.Right()");

            if (stack.Count > 1)
            {
                first = stack.Pop();
                if (first.GetType() != typeof(string))
                {
                    if (stack.Count > 0)
                    {
                        second = stack.Pop();
                        if (second.GetType() == typeof(string))
                        {
                            length = (int)Math.Truncate(Convert.ToDouble(first));
                            value = second.ToString();
                            if (length < 1)
                            {
                                value = "";
                                TraceInternal.TraceVerbose("Right: '" + value + "'");
                                stack.Push(value);
                            }
                            else if (length >= value.Length)
                            {
                                TraceInternal.TraceVerbose("Right: '" + value + "'");
                                stack.Push(value);
                            }
                            else
                            {
                                value = value.Substring(value.Length - length, length);
                                TraceInternal.TraceInformation("RIGHT(\"" + value + "\"," + length + ")");
                                TraceInternal.TraceVerbose("Right: '" + value + "'");
                                stack.Push(value);
                            }
                        }
                        else
                        {
                            Expected("string");
                        }
                    }
                }
                else
                {
                    Expected("double");
                }
            }
            Debug.WriteLine("Out Evaluator.Right()");
        }

        //---------------------------------------------------------------}
        // MID$ Top of Stack with Primary
        // 2 - to -> first
        // 1 - from -> second
        // 0 - string -> third

        private void Mid(int parameters)
        {
            object first;
            object second;
            object third;
            string value;
            int length;
            int number;

            Debug.WriteLine("In Evaluator.Mid()");

            if (parameters == 2)
            {
                if (stack.Count > 1)
                {
                    first = stack.Pop();
                    if (first.GetType() != typeof(string))
                    {
                        if (stack.Count > 0)
                        {
                            second = stack.Pop();

                            if (second.GetType() == typeof(string))
                            {
                                length = (int)Math.Truncate(Convert.ToDouble(first));
                                value = second.ToString();
                                if (length < 1)
                                {
                                    value = "";
                                    TraceInternal.TraceVerbose("Mid: '" + value + "'");
                                    stack.Push(value);
                                }
                                else if (length > value.Length)
                                {
                                    TraceInternal.TraceVerbose("Mid: '" + value + "'");
                                    stack.Push(value);
                                }
                                else
                                {
                                    TraceInternal.TraceInformation("MID(\"" + value + "\"," + length + ")");
                                    value = value.Substring(length - 1);
                                    TraceInternal.TraceVerbose("Mid: '" + value + "'");
                                    stack.Push(value);
                                }
                            }
                        }
                        else
                        {
                            Expected("string");
                        }
                    }
                    else
                    {
                        Expected("double");
                    }
                }
            }
            else
            {
                if (stack.Count > 2)
                {
                    first = stack.Pop();
                    if (first.GetType() != typeof(string))
                    {
                        if (stack.Count > 1)
                        {
                            second = stack.Pop();

                            if (second.GetType() != typeof(string))
                            {
                                if (stack.Count > 0)
                                {
                                    third = stack.Pop();

                                    if (third.GetType() == typeof(string))
                                    {
                                        number = (int)Math.Truncate(Convert.ToDouble(second));
                                        length = (int)Math.Truncate(Convert.ToDouble(first));
                                        value = third.ToString();

                                        if (number < 1)
                                        {
                                            number = 1;
                                        }
                                        else if (number >= value.Length)
                                        {
                                            number = value.Length;
                                        }

                                        if (length < 1)
                                        {
                                            value = "";
                                            TraceInternal.TraceVerbose("Mid: '" + value + "'");
                                            stack.Push(value);
                                        }
                                        else if (number + length > value.Length)
                                        {
                                            value = value.Substring(number - 1);
                                            TraceInternal.TraceVerbose("Mid: '" + value + "'");
                                            stack.Push(value);
                                        }
                                        else
                                        {
                                            TraceInternal.TraceInformation("MID(\"" + value + "\"," + number + "," + length + ")");
                                            value = value.Substring(number - 1, length);
                                            TraceInternal.TraceVerbose("Mid: '" + value + "'");
                                            stack.Push(value);
                                        }
                                    }
                                }
                                else
                                {
                                    Expected("string");
                                }
                            }
                            else
                            {
                                Expected("double");
                            }
                        }
                    }
                    else
                    {
                        Expected("double");
                    }
                }
            }
            Debug.WriteLine("Out Evaluator.Mid()");
        }

        #endregion functions
        #region Relation        

        //---------------------------------------------------------------}
        // LESS THAN Top of Stack with Primary
        void Less()
        {
            object first;
            object second;
            int compare;
			
			Debug.WriteLine("In Evaluator.Less()");
			
            if (stack.Count > 1)
            {
                first = stack.Pop();
                bool truth;
                if (first.GetType() == typeof(string))
                {
                    if (stack.Count > 0)
                    {
                        second = stack.Pop();
                        if (second.GetType() != typeof(string))
                        {
                            // only expecting an integer or double
                            Expected("string");
                        }
                        else
                        {
                            // -ve first < second, 0 first=second, +ve first > second
                            compare = string.Compare(first.ToString(), second.ToString());
                            if (compare > 0)
                            {
                                truth = true;   // first > second
                            }
                            else
                            {
                                truth = false;  // first < second or first = second
                            }
                            TraceInternal.TraceVerbose("Less: " + truth);
                            TraceInternal.TraceInformation("\"" + second + "\"<\"" + first + "\"=" + truth);
                            stack.Push(truth);
                        }
                    }
                }
                else
                {
                    if (stack.Count > 0)
                    {
                        second = stack.Pop();
                        if (second.GetType() == typeof(string))
                        {
                            // only expecting an integer or double
                            Expected("double");
                        }
                        else
                        {
                            truth = Convert.ToDouble(first) > Convert.ToDouble(second);
                            TraceInternal.TraceVerbose("Less: " + truth);
                            TraceInternal.TraceInformation(second + "<" + first + "=" + truth);
                            stack.Push(truth);
                        }
                    }
                }
            }
			Debug.WriteLine("Out Evaluator.Less()");
        }

        //---------------------------------------------------------------}
        // LESS THAN OR EQUAL Top of Stack with Primary
        void LessEqual()
        {
            object first;
            object second;
            int compare;
			
			Debug.WriteLine("In Evaluator.LessEqual()");
			
            if (stack.Count > 1)
            {
                first = stack.Pop();
                bool truth;
                if (first.GetType() == typeof(string))
                {
                    if (stack.Count > 0)
                    {
                        second = stack.Pop();
                        if (second.GetType() != typeof(string))
                        {
                            // only expecting an integer or double
                            Expected("string");
                        }
                        else
                        {
                            // -ve first < second, 0 first=second, +ve first > second
                            compare = string.Compare(first.ToString(), second.ToString());
                            if ((compare > 0) || (compare == 0))
                            {
                                truth = true;   // first > second and first = second
                            }
                            else
                            {
                                truth = false;  // first < second
                            }
                            TraceInternal.TraceVerbose("LessEqual: " + truth);
                            TraceInternal.TraceInformation("\"" + second + "\"<=\"" + first + "\"=" + truth);
                            stack.Push(truth);
                        }
                    }
                }
                else
                {
                    if (stack.Count > 0)
                    {
                        second = stack.Pop();
                        if (second.GetType() == typeof(string))
                        {
                            // only expecting an integer or double
                            Expected("double");
                        }
                        else
                        {
                            truth = Convert.ToDouble(first) >= Convert.ToDouble(second);
                            TraceInternal.TraceVerbose("LessEqual: " + truth);
                            TraceInternal.TraceInformation(second + "<=" + first + "=" + truth);
                            stack.Push(truth);
                        }
                    }
                }
            }
			Debug.WriteLine("Out Evaluator.LessEqual()");
        }

        //---------------------------------------------------------------}
        // GREATER THAN Top of Stack with Primary
        void Greater()
        {
            object first;
            object second;
            int compare;
			
			Debug.WriteLine("In Evaluator.Greater()");
			
            if (stack.Count > 1)
            {
                first = stack.Pop();
                bool truth;
                if (first.GetType() == typeof(string))
                {
                    if (stack.Count > 0)
                    {
                        second = stack.Pop();
                        if (second.GetType() != typeof(string))
                        {
                            // only expecting an integer or double
                            Expected("string");
                        }
                        else
                        {
                            // -ve first < second, 0 first=second, +ve first > second
                            compare = string.Compare(first.ToString(), second.ToString());
                            if (compare < 0)
                            {
                                truth = true;  // first < second

                            }
                            else
                            {
                                truth = false;  // first > second and first = second
                            }
                            TraceInternal.TraceVerbose("Greater: " + truth);
                            TraceInternal.TraceInformation("\"" + second + "\">\"" + first + "\"=" + truth);
                            stack.Push(truth);
                        }
                    }
                }
                else if (first.GetType() == typeof(bool))
                {
                    Expected("boolean");
                }
                else
                {
                    if (stack.Count > 0)
                    {
                        second = stack.Pop();
                        if (second.GetType() != typeof(double))
                        {
                            // only expecting an integer or double
                            Expected("double");
                        }
                        else
                        {
                            truth = Convert.ToDouble(first) < Convert.ToDouble(second);
                            TraceInternal.TraceVerbose("Greater: " + truth);
                            TraceInternal.TraceInformation(second + ">" + first + "=" + truth);
                            stack.Push(truth);
                        }
                    }
                }
            }
			Debug.WriteLine("Out Evaluator.Greater()");
        }

        //---------------------------------------------------------------}
        // GREATER THAN OR EQUAL Top of Stack with Primary
        void GreaterEqual()
        {
            object first;
            object second;
            int compare;
			
			Debug.WriteLine("In Evaluator.GreaterEqual()");
			
            if (stack.Count > 1)
            {
                first = stack.Pop();
                bool truth;
                if (first.GetType() == typeof(string))
                {
                    if (stack.Count > 0)
                    {
                        second = stack.Pop();
                        if (second.GetType() != typeof(string))
                        {
                            // only expecting an integer or double
                            Expected("string");
                        }
                        else
                        {
                            // -ve first < second, 0 first=second, +ve first > second
                            compare = string.Compare(first.ToString(), second.ToString());
                            if ((compare < 0) || (compare == 0))
                            {
                                truth = true;   // first < second and first = second
                            }
                            else
                            {
                                truth = false;  // first > second
                            }
                            TraceInternal.TraceVerbose("GreaterEqual: " + truth);
                            TraceInternal.TraceInformation("\"" + second + "\">=\"" + first + "\"=" + truth);
                            stack.Push(truth);
                        }
                    }
                }
                else
                {
                    if (stack.Count > 0)
                    {
                        second = stack.Pop();
                        if (second.GetType() == typeof(string))
                        {
                            // only expecting an integer or double
                            Expected("double");
                        }
                        else
                        {
                            truth = Convert.ToDouble(first) <= Convert.ToDouble(second);
                            TraceInternal.TraceVerbose("GreaterEqual: " + truth);
                            TraceInternal.TraceInformation(second + ">=" + first + "=" + truth);
                            stack.Push(truth);
                        }
                    }
                }
            }
			Debug.WriteLine("Out Evaluator.GreaterEqual()");
        }

        //---------------------------------------------------------------}
        // EQUAL Top of Stack with Primary
        void Equal()
        {
            object first;
            object second;
			
			Debug.WriteLine("In Evaluator.Equal()");
			
            if (stack.Count > 1)
            {
                first = stack.Pop();
                bool truth;
                if (first.GetType() == typeof(string))
                {
                    if (stack.Count > 0)
                    {
                        second = stack.Pop();
                        if (second.GetType() != typeof(string))
                        {
                            // only expecting an integer or double
                            Expected("string");
                        }
                        else
                        {
                            truth = string.Equals(first.ToString(), second.ToString());
                            TraceInternal.TraceVerbose("Equal: " + truth);
                            TraceInternal.TraceInformation("\"" + second + "\"=\"" + first + "\"=" + truth);
                            stack.Push(truth);
                        }
                    }
                }
                else
                {
                    if (stack.Count > 0)
                    {
                        second = stack.Pop();
                        if (second.GetType() == typeof(string))
                        {
                            // only expecting an integer or double
                            Expected("double");
                        }
                        else
                        {
                            truth = Convert.ToDouble(first) == Convert.ToDouble(second);
                            TraceInternal.TraceVerbose("Equal: " + truth);
                            TraceInternal.TraceInformation(second + "=" + first + "=" + truth);
                            stack.Push(truth);
                        }
                    }
                }
            }
			Debug.WriteLine("Out Evaluator.Equal()");
        }

        //---------------------------------------------------------------}
        // NOT EQUAL Top of Stack with Primary
        void NotEqual()
        {
            object first;
            object second;
			
			Debug.WriteLine("In Evaluator.NotEqual()");
			
            if (stack.Count > 1)
            {
                first = stack.Pop();
                bool truth;
                if (first.GetType() == typeof(string))
                {
                    if (stack.Count > 0)
                    {
                        second = stack.Pop();
                        if (second.GetType() != typeof(string))
                        {
                            // only expecting an integer or double
                            Expected("string");
                        }
                        else
                        {
                            truth = !string.Equals(first.ToString(), second.ToString());
                            TraceInternal.TraceVerbose("NotEqual: " + truth);
                            TraceInternal.TraceInformation("\"" + first + "\"<>\"" + second + "\"=" + truth);
                            stack.Push(truth);
                        }
                    }
                }
                else
                {
                    if (stack.Count > 0)
                    {
                        second = stack.Pop();
                        if (second.GetType() == typeof(string))
                        {
                            // only expecting an integer or double
                            Expected("double");
                        }
                        else
                        {
                            truth = Convert.ToDouble(first) != Convert.ToDouble(second);
                            TraceInternal.TraceVerbose("NotEqual: " + truth);
                            TraceInternal.TraceInformation(first + "<>" + second + "=" + truth);
                            stack.Push(truth);
                        }
                    }
                }
            }
			Debug.WriteLine("Out Evaluator.NotEqual()");
        }

        #endregion
        #region types

        //---------------------------------------------------------------}
        // BOOLEAN Top of Stack with Primary

        public Boolean PopBoolean()
        {
            object first;
            Boolean value = false;
			
			Debug.WriteLine("In Evaluator.PopBoolean()");

            if (stack.Count > 0)
            {
                first = stack.Pop();
                if ((first.GetType() == typeof(string)) || (first.GetType() == typeof(double)) || (first.GetType() == typeof(int)))
                {
                    // only expecting an integer or double
                    Expected("boolean");
                }
                else
                {
                    value = (Boolean)first;
                }
				TraceInternal.TraceVerbose("PopBoolean: " + value);
            }
            Debug.WriteLine("Out Evaluator.PopBoolean()");
            return (value);
        }

        //---------------------------------------------------------------}
        // DOUBLE Top of Stack with Primary

        public Double PopDouble()
        {
            object first;
            Double number = 0;
			
			Debug.WriteLine("In Evaluator.PopDouble()");

            if (stack.Count > 0)
            {
                first = stack.Pop();
                if ((first.GetType() == typeof(string)) || (first.GetType() == typeof(Boolean)))
                {
                    // only expecting an integer or double
                    Expected("Double");
                }
                else
                {
                    number = Convert.ToDouble(first);
                }
				TraceInternal.TraceVerbose("PopDouble: " + number);
            }
            Debug.WriteLine("Out Evaluator.PopDouble()");
            return (number);
        }

        //---------------------------------------------------------------}
        // INTEGER Top of Stack with Primary

        public int PopInteger()
        {
            object first;
            int integer = 0;
			
			Debug.WriteLine("In Evaluator.PopInteger()");

            if (stack.Count > 0)
            {
                first = stack.Pop();
                if ((first.GetType() == typeof(String)) || (first.GetType() == typeof(double)) || (first.GetType() == typeof(Boolean)))
                {
                    // only expecting an integer or double
                    Expected("integer");
                }
                else
                {
                    integer = (int)first;
                }
				TraceInternal.TraceVerbose("PopInteger: " + integer);
            }
			Debug.WriteLine("Out Evaluator.PopInteger()");
            return (integer);
        }

        //---------------------------------------------------------------}
        // STRING Top of Stack with Primary

        public String PopString()
        {
            object first;
            string value = "";
			
			Debug.WriteLine("In Evaluator.PopString()");

            if (stack.Count > 0)
            {
                first = stack.Pop();
                if ((first.GetType() == typeof(Boolean)) || (first.GetType() == typeof(double)) || (first.GetType() == typeof(int)))
                {
                    // only expecting an integer or double
                    Expected("string");
                }
                else
                {
                    value = (string)first;
                }
				TraceInternal.TraceVerbose("PopString: " + value);
            }
            Debug.WriteLine("Out Evaluator.PopString()");
            return (value);
        }

        //---------------------------------------------------------------}
        // pop OBJECT Top of Stack
        public object PopObject()
        {
            object first = null;
			Debug.WriteLine("In Evaluator.PopObject()");
            if (stack.Count > 0)
            {
                first = stack.Pop();
				TraceInternal.TraceVerbose("PopObject: " + first.ToString());
            }
			Debug.WriteLine("Out Evaluator.PopObject()");
            return (first);
        }

        #endregion types
        #region operators

        //---------------------------------------------------------------}
        // ADD Top of Stack with Primary
        void Add()
        {
            object first;
            object second;
            double number;
            string value;
			
			Debug.WriteLine("In Evaluator.Add()");

            if (stack.Count > 1)
            {
                first = stack.Pop();
                if (first.GetType() == typeof(string))
                {
                    if (stack.Count > 0)
                    {
                        second = stack.Pop();
                        if (second.GetType() == typeof(string))
                        {
                            value = second.ToString() + first.ToString();
                            TraceInternal.TraceVerbose("PopAdd: '" + second + "' + '" + first + "' =" + value);
                            stack.Push(value);
                        }
                        else
                        {
                            // only expecting a string
                            Expected("String");
                        }
                    }
                }
                else
                {
                    if (stack.Count > 0)
                    {
                        second = stack.Pop();
                        if (second.GetType() == typeof(string))
                        {
                            // only expecting an integer or double
                            Expected("double");
                        }
                        else
                        {
                            number = (double)second + (double)first;
                            TraceInternal.TraceVerbose("PopAdd: " + second + "+" + first + "=" + number);
                            stack.Push(number);
                        }
                    }
                }
            }
			Debug.WriteLine("Out Evaluator.Add()");
        }

        //---------------------------------------------------------------}
        // SUBTRACT Top of Stack with Primary
        void Subtract()
        {
            object first;
            object second;
            double number;

            Debug.WriteLine("In Evaluator.Subtract()");

            if (stack.Count > 1)
            {
                first = stack.Pop();
                if (first.GetType() == typeof(string))
                {
                    // only expecting an int or double
                    Expected("double");
                }
                else
                {
                    if (stack.Count > 0)
                    {
                        second = stack.Pop();
                        if (second.GetType() == typeof(string))
                        {
                            // only expecting an int
                            Expected("double");
                        }
                        else
                        {
                            number = (double)second - (double)first;
                            TraceInternal.TraceVerbose("PopSubtract: " + second + "-" + first + "=" + number);
                            stack.Push(number);
                        }
                    }
                }
            }
            Debug.WriteLine("Out Evaluator.Subtract()");
        }

        //---------------------------------------------------------------}
        // MULTIPLY Top of Stack with Primary
        void Multiply()
        {
            object first;
            object second;
            double numeric;

            Debug.WriteLine("In Evaluator.Multiply()");

            if (stack.Count > 1)
            {
                first = stack.Pop();
                if (first.GetType() == typeof(string))
                {
                    // only expecting an integer or double
                    Expected("double");
                }
                else
                {
                    if (stack.Count > 0)
                    {
                        second = stack.Pop();
                        if (second.GetType() == typeof(string))
                        {
                            // only expecting an integer or double
                            Expected("double");
                        }
                        else
                        {
                            numeric = (double)second * (double)first;
                            TraceInternal.TraceVerbose("PopMultiply: " + second + "*" + first + "=" + numeric);
                            stack.Push(numeric);
                        }
                    }
                }
            }
            Debug.WriteLine("Out Evaluator.Multiply()");
        }

        //---------------------------------------------------------------}
        // DIVIDE Top of Stack with Primary
        void Divide()
        {
            object first;
            object second;
            double number;

            Debug.WriteLine("In Evaluator.Divide()");

            if (stack.Count > 1)
            {
                first = stack.Pop();
                if (first.GetType() == typeof(string))
                {
                    // only expecting an integer or double
                    Expected("double");
                }
                else
                {
                    if (stack.Count > 0)
                    {
                        second = stack.Pop();
                        if (second.GetType() == typeof(string))
                        {
                            // only expecting an integer or double
                            Expected("double");
                        }
                        else
                        {
                            number = (double)second / (double)first;
                            TraceInternal.TraceVerbose("PopDivide: " + second + "/" + first + "=" + number);
                            stack.Push(number);
                        }
                    }
                }
            }
            Debug.WriteLine("Out Evaluator.Divide()");
        }

        //---------------------------------------------------------------} 
        // NOT Top of Stack with Primary
        void Not()
        {
            object first;

            Debug.WriteLine("In Evaluator.Not()");

            if (stack.Count > 0)
            {
                first = stack.Pop();
                if (first.GetType() != typeof(Boolean))
                {
                    // only expecting a boolean
                    Expected("boolean");
                }
                else
                {
                    stack.Push(!(bool)first);
                }
            }
            Debug.WriteLine("Out Evaluator.Not()");
        }

        //---------------------------------------------------------------} 
        // AND Top of Stack with Primary
        void And()
        {
            object first;
            object second;

            Debug.WriteLine("In Evaluator.And()");

            if (stack.Count > 1)
            {
                first = stack.Pop();
                if (first.GetType() != typeof(Boolean))
                {
                    // only expecting a boolean
                    Expected("boolean");
                }
                else
                {
                    if (stack.Count > 0)
                    {
                        second = stack.Pop();
                        if (second.GetType() != typeof(Boolean))
                        {
                            // only expecting a boolean
                            Expected("boolean");
                        }
                        else
                        {
                            stack.Push((Boolean)first && (Boolean)second);
                        }
                    }
                }
            }
            Debug.WriteLine("Out Evaluator.And()");
        }

        //---------------------------------------------------------------} 
        // OR Top of Stack with Primary
        void Or()
        {
            object first;
            object second;

            Debug.WriteLine("In Evaluator.Or()");

            if (stack.Count > 1)
            {
                first = stack.Pop();
                if (first.GetType() != typeof(Boolean))
                {
                    // only expecting a boolean
                    Expected("boolean");
                }
                else
                {
                    if (stack.Count > 0)
                    {
                        second = stack.Pop();
                        if (second.GetType() != typeof(Boolean))
                        {
                            // only expecting a boolean
                            Expected("boolean");
                        }
                        else
                        {
                            stack.Push((Boolean)first || (Boolean)second);
                        }
                    }
                }
            }
            Debug.WriteLine("Out Evaluator.Or()");
        }

        //---------------------------------------------------------------} 
        // OR Top of Stack with Primary
        void Xor()
        {
            object first;
            object second;

            Debug.WriteLine("In Evaluator.Xor()");

            if (stack.Count > 1)
            {
                first = stack.Pop();
                if (first.GetType() != typeof(Boolean))
                {
                    // only expecting a boolean
                    Expected("boolean");
                }
                else
                {
                    if (stack.Count > 0)
                    {
                        second = stack.Pop();
                        if (second.GetType() != typeof(Boolean))
                        {
                            // only expecting a boolean
                            Expected("boolean");
                        }
                        else
                        {
                            stack.Push((Boolean)first || (Boolean)second);
                        }
                    }
                }
            }
            Debug.WriteLine("Out Evaluator.Xor()");
        }

        //---------------------------------------------------------------}
        // POWER Top of Stack with Primary
        private void Power()
        {
            object first;
            object second;
            double number;

            Debug.WriteLine("In Evaluator.Power()");

            if (stack.Count > 1)
            {
                first = stack.Pop();
                if (first.GetType() == typeof(string))
                {
                    // only expecting an integer or double
                    Expected("double");
                }
                else
                {
                    if (stack.Count > 0)
                    {
                        second = stack.Pop();
                        if (second.GetType() == typeof(string))
                        {
                            // only expecting an integer or double
                            Expected("double");
                        }
                        else
                        {
                            number = Math.Pow((double)second, (double)first);
                            TraceInternal.TraceVerbose("PopPower: " + number);
                            stack.Push(number);
                        }
                    }
                }
            }
            Debug.WriteLine("Out Evaluator.Power()");
        }

        #endregion operators

        public int GetIntVariable(int varnum)
        {
            Debug.WriteLine("In Evaluator.GetIntVariable()");
            int integer;
            if (varnum >= 0 && varnum <= MAX_VARNUM)
            {
                integer = variables[varnum];
            }
            else
            {
                integer = 0;
            }
            TraceInternal.TraceVerbose("varNum" + varnum + " integer=" + integer);
            Debug.WriteLine("Out Evaluator.GetIntVariable()");
            return (integer);
        }

        public string GetStringVariable(string varName)
        {
            Debug.WriteLine("In Evaluator.GetStringVariable()");

            // Not sure what happens if the variable doesnt exit
            // think this should error but wonder what the specification says

            string value;
            if (stringVariables.ContainsKey(varName))
            {
                value = (string)stringVariables[varName];
            }
            else
            {
                value = "";
            }
            TraceInternal.TraceVerbose("varName=" + varName + " value=" + value);
            Debug.WriteLine("Out Evaluator.GetStringVariable()");
            return (value);
        }

        public double GetNumericVariable(string varName)
        {
            double number;
            Debug.WriteLine("In Evaluator.GetNumericVariable()");
            if (numericVariables.ContainsKey(varName))
            {
                number = (double)numericVariables[varName];
            }
            else
            {
                number = 0;
            }
            TraceInternal.TraceVerbose("varName=" + varName + " number=" + number);
            Debug.WriteLine("Out Evaluator.GetNumericVariable()");
            return (number);
        }

        public double GetNumericArrayVariable(string varName, int positions, int[] position)
        {
            Debug.WriteLine("In Evaluator.GetNumericArrayVariable()");

            BasicLibrary.Array data;
            double number;
            if (numericArrayVariables.ContainsKey(varName))
            {
                data = (BasicLibrary.Array)numericArrayVariables[varName];
                number = (double)data.Get(position);
            }
            else
            {
                number = 0;
            }
            TraceInternal.TraceVerbose("varName=" + varName + " number=" + number);
            Debug.WriteLine("Out Evaluator.GetNumericArrayVariable()");
            return (number);
        }

        public string GetStringArrayVariable(string varName, int positions, int[] position)
        {
            Debug.WriteLine("In Evaluator.GetStringArrayVariable()");

            BasicLibrary.Array data;
            string value;
            if (stringArrayVariables.ContainsKey(varName))
            {
                data = (BasicLibrary.Array)stringArrayVariables[varName];
                value = (string)data.Get(position);
            }
            else
            {
                value = "";
            }
            TraceInternal.TraceVerbose("varName=" + varName + " value=" + value);
            Debug.WriteLine("In Evaluator.GetStringArrayVariable()");
            return (value);
        }

        public void DeclareNumericArrayVariable(string varName, int dimensions, int[] dimension)
        {
            Debug.WriteLine("In Evaluator.DeclareNumericArrayVariable()");
            BasicLibrary.Array data;
            if (numericArrayVariables.ContainsKey(varName))
            {
                Expected("Array already defined " + varName + "(");
            }
            data = new BasicLibrary.Array(varName, dimensions, dimension,(double)0);
            numericArrayVariables.Add(varName, data);
            Debug.WriteLine("In Evaluator.DeclareNumericArrayVariable()");
        }

        public void DeclareStringArrayVariable(string varName, int dimensions, int[] dimension)
        {
            Debug.WriteLine("In Evaluator.DeclareStringArrayVariable()");
            BasicLibrary.Array data;
            if (stringArrayVariables.ContainsKey(varName))
            {
                Expected("Array already defined " + varName + "(");
            }
            data = new BasicLibrary.Array(varName, dimensions, dimension, (string)"");
            stringArrayVariables.Add(varName, data);
            Debug.WriteLine("Out Evaluator.DeclareStringArrayVariable()");
        }

        public void SetIntVariable(int varnum, int integer)
        {
            Debug.WriteLine("In Evaluator.SetIntVariable()");
            if (varnum >= 0 && varnum <= MAX_VARNUM)
            {
                variables[varnum] = integer;
            }
            TraceInternal.TraceVerbose("varNum=" + varnum + " integer=" + integer);
            Debug.WriteLine("Out Evaluator.SetIntVariable()");
        }

        public void SetStringVariable(string varName, string value)
        {
            Debug.WriteLine("In Evaluator.SetStringVariable()");
            if (stringVariables.ContainsKey(varName))
            {
                stringVariables.Remove(varName);
            }
            stringVariables.Add(varName, value);
            TraceInternal.TraceVerbose("varName=" + varName + " value=" + value);
            Debug.WriteLine("Out Evaluator.SetStringVariable()");
        }

        public void SetNumericVariable(string varName, double number)
        {
            Debug.WriteLine("In Evaluator.SetNumericVariable()");
            if (numericVariables.ContainsKey(varName))
            {
                numericVariables.Remove(varName);
            }
            numericVariables.Add(varName, number);
            TraceInternal.TraceVerbose("varName=" + varName + " number=" + number);
            Debug.WriteLine("Out Evaluator.SetNumericVariable()");
        }

        public void SetNumericArrayVariable(string varName, int positions, int[] position, double number)
        {
            Debug.WriteLine("In Evaluator.SetNumericArrayVariable()");
            BasicLibrary.Array data;
            if (!numericArrayVariables.ContainsKey(varName))
            {
                // it appears that if no DIM then defaults to 1 dimension, and 11 elements (0-10)
                int[] dimension = new int[2];
                dimension[1] = 10;
                DeclareNumericArrayVariable(varName, positions, dimension);
            }
            data = (BasicLibrary.Array)numericArrayVariables[varName];
            data.Set(position, number);
        
            TraceInternal.TraceVerbose("varName=" + varName + " number=" + number);
            Debug.WriteLine("Out Evaluator.SetNumericArrayVariable()");
        }

        public void SetStringArrayVariable(string varName, int positions, int[] position, string value)
        {
            Debug.WriteLine("In Evaluator.SetStringArrayVariable()");
            BasicLibrary.Array data;
            if (!stringArrayVariables.ContainsKey(varName))
            {
                // it appears that if no DIM then defaults to 1 dimension, and 11 elements (0-10)
                int[] dimension = new int[2];
                dimension[1] = 10;
                DeclareStringArrayVariable(varName, positions, dimension);
            }
            data = (BasicLibrary.Array)stringArrayVariables[varName];
            data.Set(position, value);

            TraceInternal.TraceVerbose("varName=" + varName + " value=" + value);
            Debug.WriteLine("Out Evaluator.SetStringArrayVariable()");
        }

        #endregion
        #region Private

       
        //--------------------------------------------------------------
        // Report What Was Expected 

        public void Expected(string s)
        {
            throw new System.ArgumentException("Unexpected", s + " expected @");
        }

        #endregion
    }
}
