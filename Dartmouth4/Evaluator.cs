// Copyright (C) 1988 Jack W. Crenshaw. All rights reserved. 

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using log4net;
using uBasicLibrary;

namespace Dartmouth4
{
    public class Evaluator
    {
        #region Variables

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //protected System.IO.TextReader In = null;
        //protected System.IO.TextWriter Out = null;
        //protected System.IO.TextWriter Error = null;

        Stack<object> stack;

        private Tokenizer tokenizer;

        const int MAX_VARNUM = 26;
        int[] variables = new int[MAX_VARNUM];
        Hashtable stringVariables;
        Hashtable numericVariables;
        Hashtable numericArrayVariables;
        Hashtable stringArrayVariables;

        // functions

        public struct FunctionIndex
        {
            private int programTextPosition;
            private int @params;
            private string[] param;

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

        #endregion
        #region Constructors

        public Evaluator(Tokenizer tokenizer)
        {
            stack = new Stack<object>();
            this.tokenizer = tokenizer;
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
            Debug("Randomize: Enter");
            randomize = Environment.TickCount;
            Debug("Randomize: Enter");
        }




        // <relation>      ::= | <expression> [<relop> <expression]
        // <expression>    ::= <term> [<addop> <term>]*
        // <term>          ::= <signed factor> [<mulop> factor]*
        // <signed factor> ::= [<addop>] <factor>
        // <factor>        ::= <integer> | <variable> | (<expression>)
        /// <summary>
        /// Relation
        /// </summary>
        /// <returns></returns>
        public void Relation()
        {
            Tokenizer.Token op;

            Debug("Relation: Enter");
            Expression();
            op = tokenizer.GetToken();

            Debug("relation: token " + Convert.ToString(op));
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
            Debug("Relation: Exit");
        }

        /// <summary>
        /// Expression
        /// </summary>
        /// <returns></returns>
        public void Expression()
        {
            Tokenizer.Token op;
            Debug("Expression: Enter");

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
            Debug("Expression: token " + Convert.ToString(op));
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
            Debug("Expression: exit");
        }

        /// <summary>
        /// Term
        /// </summary>
        /// <returns></returns>
        private void Term()
        {
            Tokenizer.Token op;
            Debug("Term: Enter");

            Debug("Term: token " + tokenizer.GetToken());
            Exponent();
            op = tokenizer.GetToken();
            Debug("Term: token " + op);

            while (op == Tokenizer.Token.TOKENIZER_ASTR || op == Tokenizer.Token.TOKENIZER_SLASH || op == Tokenizer.Token.TOKENIZER_MOD)
            {
                tokenizer.NextToken();
                Debug("Term: token " + tokenizer.GetToken());
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
            Debug("Term: Exit");
        }

        /// <summary>
        /// Exponent
        /// </summary>
        /// <returns></returns>
        private void Exponent()
        {
            Tokenizer.Token op;
            Debug("Exponent: Enter");

            Debug("Exponent: token " + tokenizer.GetToken());
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
            Debug("Exponent: token " + op);
            while (op == Tokenizer.Token.TOKENIZER_EXPONENT)
            {
                tokenizer.NextToken();
                Debug("Exponent: token " + tokenizer.GetToken());
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
            Debug("Exponent: Exit");
        }

        /// <summary>
        /// Factor
        /// </summary>
        private void Factor()
        {
            object f;
            string varName = "";
            FunctionIndex function;
            int num = 0;

            Debug("Factor: Enter");

            Debug("Factor: token " + tokenizer.GetToken());
            switch (tokenizer.GetToken())
            {
                case Tokenizer.Token.TOKENIZER_FN:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_FN);
                        varName = tokenizer.GetNumericArrayVariable();
                        Debug("Factor: function " + varName);
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
                                Expression();
                                // this will be left the stack in reverse order
                            }
                        }
                        while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);

                        // assign the expressions to the variables in the correct order

                        for (int i = function.Parameters - 1; i >= 0; i--)
                        {
                            f = PopDouble();
                            Debug("Factor: function numeric " + Convert.ToString(f));
                            SetNumericVariable(function.Parameter[i], (double)f);
                        }

                        // now jump to the function execute and then restore the position and continue 

                        int current_pos = tokenizer.GetPosition();
                        tokenizer.Init(function.ProgramTextPosition);
                        Expression();
                        tokenizer.Init(current_pos);
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_ABS:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_ABS);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        Expression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Abs();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_ATN:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_ATN);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        Expression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Atn();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_COS:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_COS);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        Expression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Cos();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_EXP:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_EXP);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        Expression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Exp();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_INT:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_INT);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        Expression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Int();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_LOG:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LOG);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        Expression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Log();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_RND:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RND);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        Expression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Rnd();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_SIN:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_SIN);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        Expression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Sin();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_SQR:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_SQR);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        Expression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        SquareRoot();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_TAN:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_TAN);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        Expression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        Tan();
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_NUMBER:
                    {
                        f = tokenizer.GetNumber();
                        Debug("Factor: number " + Convert.ToString(f));
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_NUMBER);
                        stack.Push(f);
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_INTEGER:
                    {
                        f = (double)tokenizer.GetInteger();
                        Debug("Factor: integer " + Convert.ToString(f));
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_INTEGER);
                        stack.Push(f);
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_STRING:
                    {
                        f = tokenizer.Getstring();
                        Debug("Factor: string '" + Convert.ToString(f) + "'");
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STRING);
                        stack.Push((string)f);
                        break;
                    }

                case Tokenizer.Token.TOKENIZER_LEFTPAREN:
                    {
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_LEFTPAREN);
                        Expression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_STRING_VARIABLE:
                    {
                        f = GetStringVariable(tokenizer.GetStringVariable());
                        Debug("Factor: string variable '" + Convert.ToString(f) + "'");
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_STRING_VARIABLE);
                        stack.Push(f);
                        break;
                    }
                case Tokenizer.Token.TOKENIZER_NUMERIC_VARIABLE:
                    {
                        f = GetNumericVariable(tokenizer.GetNumericVariable());
                        Debug("Factor: numeric variable " + Convert.ToString(f));
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
                                Expression();
                                numeric = (int)Math.Truncate(PopDouble());
                                dimension = dimension + 1;
                                dimensions[dimension] = numeric;
                            }
                        }
                        while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);

                        f = GetNumericArrayVariable(varName, dimension, dimensions);
                        Debug("Factor: numeric array " + Convert.ToString(f));
                        stack.Push(f);
                        break;
                    }

                case Tokenizer.Token.TOKENIZER_STRING_ARRAY_VARIABLE:
                    {
                        int numeric;
                        int dimension = 0;
                        int[] dimensions = new int[10];
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
                                Expression();
                                numeric = (int)Math.Truncate(PopDouble());
                                dimension = dimension + 1;
                                dimensions[dimension] = numeric;
                            }
                        }
                        while (tokenizer.GetToken() != Tokenizer.Token.TOKENIZER_RIGHTPAREN);
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);

                        f = GetStringArrayVariable(varName, dimension, dimensions);
                        Debug("Factor: string array " + Convert.ToString(f));
                        stack.Push(f);
                        break;
                    }

                default:
                    {
                        num = tokenizer.GetIntegerVariable();
                        f = GetIntVariable(num);
                        Debug("Factor: int " + Convert.ToString(f));
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_INTEGER);
                        stack.Push(f);
                        break;
                    }
            }
            Debug("Factor: Exit");
        }



        #region functions

        //---------------------------------------------------------------}
        // SQRT Top of Stack with Primary
        private void SquareRoot()
        {
            object first;
            double number;
            Debug("SquareRoot: Enter");

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
                        Debug("PopSqr: " + number);
                        stack.Push(number);
                    }
                    else
                    {
                        Expected("positive");
                    }
                }
            }
            Debug("SquareRoot: Exit");
        }

        //---------------------------------------------------------------}
        // ABS Top of Stack with Primary
        private void Abs()
        {
            // This just removes the ecimal part with no rounding acording to the specification

            object first;
            double number;
            Debug("Abs: Enter");

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
                    Debug("Abs: " + number);
                    stack.Push(number);
                }
            }
            Debug("Abs: Exit");
        }

        //---------------------------------------------------------------}
        // INT Top of Stack with Primary
        private void Int()
        {
            // This just removes the decimal part with no rounding acording to the specification

            object first;
            double number;
            Debug("Int: Enter");

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
                    Debug("Int: " + number);
                    stack.Push(number);
                }
            }
            Debug("Int: Exit");
        }

        //---------------------------------------------------------------}
        // RND Top of Stack with Primary
        private void Rnd()
        {
            object first;
            double number;
            Debug("Rnd: Enter");

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
                    randomize = randomize - 1;
                    number = r.NextDouble();
                    Debug("Rnd: " + number);
                    stack.Push(number);
                }
            }
            Debug("Rnd: Exit");
        }

        //---------------------------------------------------------------}
        // SIN Top of Stack with Primary
        private void Sin()
        {
            object first;
            Debug("Sin: Enter");

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
            Debug("Sin: Exit");
        }

        //---------------------------------------------------------------}
        // COS Top of Stack with Primary
        private void Cos()
        {
            object first;
            Debug("Cos: Enter");
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
            Debug("Cos: Exit");
        }

        //---------------------------------------------------------------}
        // TAN Top of Stack with Primary
        private void Tan()
        {
            object first;
            Debug("Tan: Enter");

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
            Debug("Tan: Exit");
        }

        //---------------------------------------------------------------}
        // ATN Top of Stack with Primary
        private void Atn()
        {
            object first;
            Debug("Atn: Enter");

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
            Debug("Atn: Exit");
        }

        //---------------------------------------------------------------}
        // ATN Top of Stack with Primary
        private void Exp()
        {
            object first;
            Debug("Exp: Enter");

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
            Debug("Exp: Exit");
        }

        //---------------------------------------------------------------}
        // ATN Top of Stack with Primary
        private void Log()
        {
            object first;
            Debug("Log: Enter");

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
            Debug("Log: Exit");
        }

        //---------------------------------------------------------------}
        // ASC Top of Stack with Primary
        private void Asc()
        {
            object first;
            double number = 0;
            string text = "";
            Debug("Asc: Enter");

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
                    text = Convert.ToString(first);
                    if (text.Length > 0)
                    {
                        byte[] asciiBytes = Encoding.ASCII.GetBytes(text);
                        number = (double)asciiBytes[0];
                    }
                    stack.Push(number);
                }
            }
            Debug("Asc: Exit");
        }

        //---------------------------------------------------------------}
        // LEN Top of Stack 

        private void Len()
        {
            object first;
            double number=0;
            Debug("Len: Enter");

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
                    number = first.ToString().Length;
                    stack.Push(number);
                }
            }
            Debug("Len: Exit");
        }

        //---------------------------------------------------------------}
        // LEFT$ Top of Stack
        // 1 - length -> first
        // 0 - string -> second

        private void Left()
        {
            object first;
            object second;
            string value;
            int length;

            Debug("Left: Enter");

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
                                Debug("Left: '" + value + "'");
                                stack.Push(value);
                            }
                            else if (length >= value.Length)
                            {
                                Debug("Left: '" + value + "'");
                                stack.Push(value);
                            }
                            else
                            {
                                value = value.Substring(0, length);
                                Info("LEFT(\"" + value + "\"," + length + ")");
                                Debug("Left: '" + value + "'");
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
            Debug("Left: Exit");
        }

        //---------------------------------------------------------------}
        // RIGHT$ Top of Stack
        // 1 - length -> first
        // 0 - string -> second

        private void Right()
        {
            object first;
            object second;
            string value;
            int length;
            Debug("Right: Enter");

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
                                Debug("Right: '" + value + "'");
                                stack.Push(value);
                            }
                            else if (length >= value.Length)
                            {
                                Debug("Right: '" + value + "'");
                                stack.Push(value);
                            }
                            else
                            {
                                value = value.Substring(value.Length - length, length);
                                Info("RIGHT(\"" + value + "\"," + length + ")");
                                Debug("Right: '" + value + "'");
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
            Debug("Right: Exit");
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
            Boolean truth = false;

            if (stack.Count > 1)
            {
                first = stack.Pop();
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
                            Debug("Less: " + truth);
                            Info("\"" + second + "\"<\"" + first + "\"=" + truth);
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
                            Debug("Less: " + truth);
                            Info(second + "<" + first + "=" + truth);
                            stack.Push(truth);
                        }
                    }
                }
            }
        }

        //---------------------------------------------------------------}
        // LESS THAN OR EQUAL Top of Stack with Primary
        void LessEqual()
        {
            object first;
            object second;
            int compare;
            Boolean truth = false;

            if (stack.Count > 1)
            {
                first = stack.Pop();
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
                            Debug("LessEqual: " + truth);
                            Info("\"" + second + "\"<=\"" + first + "\"=" + truth);
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
                            Debug("LessEqual: " + truth);
                            Info(second + "<=" + first + "=" + truth);
                            stack.Push(truth);
                        }
                    }
                }
            }
        }

        //---------------------------------------------------------------}
        // GREATER THAN Top of Stack with Primary
        void Greater()
        {
            object first;
            object second;
            int compare;
            Boolean truth = false;

            if (stack.Count > 1)
            {
                first = stack.Pop();
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
                            Debug("Greater: " + truth);
                            Info("\"" + second + "\">\"" + first + "\"=" + truth);
                            stack.Push(truth);
                        }
                    }
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
                            Debug("Greater: " + truth);
                            Info(second + ">" + first + "=" + truth);
                            stack.Push(truth);
                        }
                    }
                }
            }
        }

        //---------------------------------------------------------------}
        // GREATER THAN OR EQUAL Top of Stack with Primary
        void GreaterEqual()
        {
            object first;
            object second;
            int compare;
            Boolean truth = false;

            if (stack.Count > 1)
            {
                first = stack.Pop();
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
                            Debug("GreaterEqual: " + truth);
                            Info("\"" + second + "\">=\"" + first + "\"=" + truth);
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
                            Debug("GreaterEqual: " + truth);
                            Info(second + ">=" + first + "=" + truth);
                            stack.Push(truth);
                        }
                    }
                }
            }
        }

        //---------------------------------------------------------------}
        // EQUAL Top of Stack with Primary
        void Equal()
        {
            object first;
            object second;
            Boolean truth = false;

            if (stack.Count > 1)
            {
                first = stack.Pop();
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
                            Debug("Equal: " + truth);
                            Info("\"" + second + "\"=\"" + first + "\"=" + truth);
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
                            Debug("Equal: " + truth);
                            Info(second + "=" + first + "=" + truth);
                            stack.Push(truth);
                        }
                    }
                }
            }
        }

        //---------------------------------------------------------------}
        // NOT EQUAL Top of Stack with Primary
        void NotEqual()
        {
            object first;
            object second;
            Boolean truth = false;

            if (stack.Count > 1)
            {
                first = stack.Pop();
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
                            Debug("NotEqual: " + truth);
                            Info("\"" + first + "\"<>\"" + second + "\"=" + truth);
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
                            Debug("NotEqual: " + truth);
                            Info(first + "<>" + second + "=" + truth);
                            stack.Push(truth);
                        }
                    }
                }
            }
        }

        #endregion

        #region types

        //---------------------------------------------------------------}
        // pop BOOLEAN Top of Stack
        public Boolean PopBoolean()
        {
            object first;
            Boolean value = false;

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
            }
            Debug("PopBoolean: " + value);
            return (value);
        }

        //---------------------------------------------------------------}
        // pop DOUBLE Top of Stack
        public Double PopDouble()
        {
            object first;
            Double number = 0;

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
            }
            Debug("PopDouble: " + number);
            return (number);
        }

        //---------------------------------------------------------------}
        // pop INTEGER Top of Stack
        public int PopInteger()
        {
            object first;
            int integer = 0;

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
            }
            Debug("PopInteger: " + integer);
            return (integer);
        }

        //---------------------------------------------------------------}
        // pop STRING Top of Stack
        public String PopString()
        {
            object first;
            string value = "";

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
            }
            Debug("PopString: " + value);
            return (value);
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
                            Debug("PopAdd: '" + second + "' + '" + first + "' =" + value);
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
                            Debug("PopAdd: " + second + "+" + first + "=" + number);
                            stack.Push(number);
                        }
                    }
                }
            }
        }

        //---------------------------------------------------------------}
        // SUBTRACT Top of Stack with Primary
        void Subtract()
        {
            object first;
            object second;
            double number;

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
                            Debug("PopSubtract: " + second + "-" + first + "=" + number);
                            stack.Push(number);
                        }
                    }
                }
            }
        }

        //---------------------------------------------------------------}
        // MULTIPLY Top of Stack with Primary
        void Multiply()
        {
            object first;
            object second;
            double numeric;

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
                            Debug("PopMultiply: " + second + "*" + first + "=" + numeric);
                            stack.Push(numeric);
                        }
                    }
                }
            }
        }

        //---------------------------------------------------------------}
        // DIVIDE Top of Stack with Primary
        void Divide()
        {
            object first;
            object second;
            double number;

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
                            Debug("PopDivide: " + second + "/" + first + "=" + number);
                            stack.Push(number);
                        }
                    }
                }
            }
        }

        //---------------------------------------------------------------} 
        // AND Top of Stack with Primary
        void And()
        {
            object first;
            object second;

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
        }

        //---------------------------------------------------------------} 
        // OR Top of Stack with Primary
        void Or()
        {
            object first;
            object second;

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
        }

        //---------------------------------------------------------------}
        // POWER Top of Stack with Primary
        private void Power()
        {
            object first;
            object second;
            double number;

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
                            Debug("PopPower: " + number);
                            stack.Push(number);
                        }
                    }
                }
            }
        }

        #endregion operators

        public int GetIntVariable(int varnum)
        {
            int integer = 0;
            Debug("GetIntVariable: Enter");
            if (varnum >= 0 && varnum <= MAX_VARNUM)
            {
                integer = variables[varnum];
            }
            else
            {
                integer = 0;
            }
            Debug("varNum" + varnum + " integer=" + integer);
            Debug("GetIntVariable: Exit");
            return (integer);
        }

        public string GetStringVariable(string varName)
        {
            // Not sure what happens if the variable doesnt exit
            // think this should error but wonder what the specification says

            string value = "";
            Debug("GetStringVariable: Enter");

            if (stringVariables.ContainsKey(varName))
            {
                value = (string)stringVariables[varName];
            }
            else
            {
                value = "";
            }
            Debug("varName=" + varName + " value=" + value);
            Debug("GetStringVariable: Exist");
            return (value);
        }

        public double GetNumericVariable(string varName)
        {
            double number = 0;
            Debug("GetNumericVariable: Enter");
            if (numericVariables.ContainsKey(varName))
            {
                number = (double)numericVariables[varName];
            }
            else
            {
                number = 0;
            }
            Debug("varName=" + varName + " number=" + number);
            Debug("GetNumericVariable: Exit");
            return (number);
        }

        public double GetNumericArrayVariable(string varName, int positions, int[] position)
        {
            uBasicLibrary.Array data;
            double number = 0;
            Debug("GetNumericArrayVariable: Enter");

            if (numericArrayVariables.ContainsKey(varName))
            {
                data = (uBasicLibrary.Array)numericArrayVariables[varName];
                number = (double)data.Get(position);
            }
            else
            {
                number = 0;
            }
            Debug("varName=" + varName + " number=" + number);
            Debug("GetNumericArrayVariable: Exit");
            return (number);
        }

        public string GetStringArrayVariable(string varName, int positions, int[] position)
        {
            uBasicLibrary.Array data;
            string value = "";
            Debug("GetNumericStringVariable: Enter");

            if (stringArrayVariables.ContainsKey(varName))
            {
                data = (uBasicLibrary.Array)stringArrayVariables[varName];
                value = (string)data.Get(position);
            }
            else
            {
                value = "";
            }
            Debug("varName=" + varName + " value=" + value);
            Debug("GetNumericStringVariable: Exit");
            return (value);
        }

        public void DeclareNumericArrayVariable(string varName, int dimensions, int[] dimension)
        {
            Debug("DeclareNumericArrayVariable: Enter");
            uBasicLibrary.Array data;
            if (numericArrayVariables.ContainsKey(varName))
            {
                Expected("Array already defined " + varName + "(");
            }
            data = new uBasicLibrary.Array(varName, dimensions, dimension,(double)0);
            numericArrayVariables.Add(varName, data);
            Debug("DeclareNumericArrayVariable: Exit");
        }

        public void DeclareStringArrayVariable(string varName, int dimensions, int[] dimension)
        {
            Debug("DeclareStringArrayVariable: Enter");
            uBasicLibrary.Array data;
            if (stringArrayVariables.ContainsKey(varName))
            {
                Expected("Array already defined " + varName + "(");
            }
            data = new uBasicLibrary.Array(varName, dimensions, dimension, (string)"");
            stringArrayVariables.Add(varName, data);
            Debug("DeclareStringArrayVariable: Exit");
        }

        public void SetIntVariable(int varnum, int integer)
        {
            Debug("SetIntVariable: Enter");
            if (varnum >= 0 && varnum <= MAX_VARNUM)
            {
                variables[varnum] = integer;
            }
            Debug("varNum=" + varnum + " integer=" + integer);
            Debug("SetIntVariable: Enter");
        }

        public void SetStringVariable(string varName, string value)
        {
            Debug("SetStringVariable: Enter");
            if (stringVariables.ContainsKey(varName))
            {
                stringVariables.Remove(varName);
            }
            stringVariables.Add(varName, value);
            Debug("varName=" + varName + " value=" + value);
            Debug("SetStringVariable: Exit");
        }

        public void SetNumericVariable(string varName, double number)
        {
            Debug("SetNumericVariable: Enter");
            if (numericVariables.ContainsKey(varName))
            {
                numericVariables.Remove(varName);
            }
            numericVariables.Add(varName, number);
            Debug("varName=" + varName + " number=" + number);
            Debug("SetNumericVariable: Exit");
        }

        public void SetNumericArrayVariable(string varName, int positions, int[] position, double number)
        {
            Debug("SetNumericArrayVariable: Enter");
            uBasicLibrary.Array data;
            if (!numericArrayVariables.ContainsKey(varName))
            {
                // it apperas that if no DIM then defaults to 10 items
                int[] dimension = new int[10];
                dimension[0] = 1;
                DeclareNumericArrayVariable(varName, positions, dimension);
            }
            data = (uBasicLibrary.Array)numericArrayVariables[varName];
            data.Set(position, number);
        
            Debug("varName=" + varName + " number=" + number);
            Debug("SetNumericArrayVariable: Exit");
        }

        public void SetStringArrayVariable(string varName, int positions, int[] position, string value)
        {
            Debug("SetStringArrayVariable: Enter");
            uBasicLibrary.Array data;
            if (!stringArrayVariables.ContainsKey(varName))
            {
                // it apperas that if no DIM then defaults to 10 items
                int[] dimension = new int[10];
                dimension[0] = 1;
                DeclareStringArrayVariable(varName, positions, dimension);
            }
            data = (uBasicLibrary.Array)stringArrayVariables[varName];
            data.Set(position, value);

            Debug("varName=" + varName + " value=" + value);
            Debug("SetStringArrayVariable: Enter");
        }

        #endregion

        //--------------------------------------------------------------
        // Debug

        void Debug(string s)
        {
            log.Debug(s);
        }

        //--------------------------------------------------------------
        // Info

        void Info(string s)
        {
            log.Info(s);
        }

        //--------------------------------------------------------------
        // Report What Was Expected 

        public void Expected(string s)
        {
            throw new System.ArgumentException("Unexpected", s + " expected @");
        }
    }
}
