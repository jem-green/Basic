// Copyright (C) 1988 Jack W. Crenshaw. All rights reserved. 

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using log4net;
using uBasicLibrary;
using System.Diagnostics;
using TracerLibrary;

namespace Dartmouth2
{
    public class Evaluator
    {
        #region Fields

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //protected System.IO.TextReader In = null;
        //protected System.IO.TextWriter Out = null;
        //protected System.IO.TextWriter Error = null;

        readonly Stack<object> stack;

        private readonly Tokenizer tokenizer;

        const int MAX_VARNUM = 26;
        readonly int[] variables = new int[MAX_VARNUM];
        readonly Hashtable numericVariables;
        readonly Hashtable numericArrayVariables;


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

        #endregion
        #region Constructors

        public Evaluator(Tokenizer tokenizer)
        {
            stack = new Stack<object>();
            this.tokenizer = tokenizer;
            numericVariables = new Hashtable();
            numericArrayVariables = new Hashtable();
            functions = new FunctionIndex[MAX_FUNCTIONS];
        }

        #endregion Constructors
        #region Properties



        #endregion Properties
        #region Methods

        public void Randomize()
        {
            Debug.WriteLine("In Randomize()");
            randomize = Environment.TickCount;
            Debug.WriteLine("Out Randomize()");
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

            Debug.WriteLine("In Relation()");
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
            Debug.WriteLine("Out Relation()");
        }

        /// <summary>
        /// Expression
        /// </summary>
        public void Expression()
        {
            Tokenizer.Token op;
            Debug.WriteLine("In Expression()");

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
            Debug.WriteLine("Out Expression()");
        }

        /// <summary>
        /// Term
        /// </summary>
        /// <returns></returns>
        private void Term()
        {
            Debug.WriteLine("In Term()");
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
            Debug.WriteLine("Out Term()");
        }

        /// <summary>
        /// Exponent
        /// </summary>
        /// <returns></returns>
        private void Exponent()
        {
            Tokenizer.Token op;
            Debug.WriteLine("In Exponent()");

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
            Debug.WriteLine("Out Exponent()");
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

            Debug.WriteLine("In Factor()");

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
                            TraceInternal.TraceVerbose("Factor: function numeric " + Convert.ToString(f));
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
                        Expression();
                        tokenizer.AcceptToken(Tokenizer.Token.TOKENIZER_RIGHTPAREN);
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
                                Expression();
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
            Debug.WriteLine("Out Factor()");
        }

        #region functions

        //---------------------------------------------------------------}
        // SQRT Top of Stack with Primary
        private void SquareRoot()
        {
            object first;
            double number;
            Debug.WriteLine("In SquareRoot()");

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
            Debug.WriteLine("Out SquareRoot()");
        }

        //---------------------------------------------------------------}
        // ABS Top of Stack with Primary
        private void Abs()
        {
            // This just removes the ecimal part with no rounding acording to the specification

            object first;
            double number;
            Debug.WriteLine("In Abs()");

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
            Debug.WriteLine("Out Abs()");
        }

        //---------------------------------------------------------------}
        // INT Top of Stack with Primary
        private void Int()
        {
            // This just removes the decimal part with no rounding acording to the specification

            object first;
            double number;
            Debug.WriteLine("In Int()");

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
                    TraceInternal.TraceVerbose("Int: " + number);
                    stack.Push(number);
                }
            }
            Debug.WriteLine("Out Int()");
        }

        //---------------------------------------------------------------}
        // RND Top of Stack with Primary
        private void Rnd()
        {
            object first;
            double number;
            Debug.WriteLine("In Rnd()");

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
            Debug.WriteLine("Out Rnd()");
        }

        //---------------------------------------------------------------}
        // SIN Top of Stack with Primary
        private void Sin()
        {
            object first;
            Debug.WriteLine("In Sin()");

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
            Debug.WriteLine("Out Sin()");
        }

        //---------------------------------------------------------------}
        // COS Top of Stack with Primary
        private void Cos()
        {
            object first;
            Debug.WriteLine("In Cos()");
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
            Debug.WriteLine("Out Cos()");
        }

        //---------------------------------------------------------------}
        // TAN Top of Stack with Primary
        private void Tan()
        {
            object first;
            Debug.WriteLine("In Tan()");

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
            Debug.WriteLine("Out Tan()");
        }

        //---------------------------------------------------------------}
        // ATN Top of Stack with Primary
        private void Atn()
        {
            object first;
            Debug.WriteLine("In Atn()");

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
            Debug.WriteLine("Out Atn()");
        }

        //---------------------------------------------------------------}
        // ATN Top of Stack with Primary
        private void Exp()
        {
            object first;
            Debug.WriteLine("In Exp()");

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
            Debug.WriteLine("Out Exp()");
        }

        //---------------------------------------------------------------}
        // ATN Top of Stack with Primary
        private void Log()
        {
            object first;
            Debug.WriteLine("In Log()");

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
            Debug.WriteLine("Out Log()");
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
			
			Debug.WriteLine("In Less()");
			
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
			Debug.WriteLine("Out Less()");
        }

        //---------------------------------------------------------------}
        // LESS THAN OR EQUAL Top of Stack with Primary
        void LessEqual()
        {
            object first;
            object second;
            int compare;
			
			Debug.WriteLine("In LessEqual()");
			
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
			Debug.WriteLine("Out LessEqual()");
        }

        //---------------------------------------------------------------}
        // GREATER THAN Top of Stack with Primary
        void Greater()
        {
            object first;
            object second;
            int compare;
			
			Debug.WriteLine("In Greater()");
			
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
			Debug.WriteLine("Out Greater()");
        }

        //---------------------------------------------------------------}
        // GREATER THAN OR EQUAL Top of Stack with Primary
        void GreaterEqual()
        {
            object first;
            object second;
            int compare;
			
			Debug.WriteLine("In GreaterEqual()");
			
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
			Debug.WriteLine("Out GreaterEqual()");
        }

        //---------------------------------------------------------------}
        // EQUAL Top of Stack with Primary
        void Equal()
        {
            object first;
            object second;
			
			Debug.WriteLine("In Equal()");
			
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
			Debug.WriteLine("Out Equal()");
        }

        //---------------------------------------------------------------}
        // NOT EQUAL Top of Stack with Primary
        void NotEqual()
        {
            object first;
            object second;
			
			Debug.WriteLine("In NotEqual()");
			
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
			Debug.WriteLine("Out NotEqual()");
        }

        #endregion
        #region types

        //---------------------------------------------------------------}
        // BOOLEAN Top of Stack with Primary

        public Boolean PopBoolean()
        {
            object first;
            Boolean value = false;
			
			Debug.WriteLine("In PopBoolean()");

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
            Debug.WriteLine("Out PopBoolean()");
            return (value);
        }

        //---------------------------------------------------------------}
        // DOUBLE Top of Stack with Primary

        public Double PopDouble()
        {
            object first;
            Double number = 0;
			
			Debug.WriteLine("In PopDouble()");

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
            Debug.WriteLine("Out PopDouble()");
            return (number);
        }

        //---------------------------------------------------------------}
        // INTEGER Top of Stack with Primary

        public int PopInteger()
        {
            object first;
            int integer = 0;
			
			Debug.WriteLine("In PopInteger()");

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
			Debug.WriteLine("Out PopInteger()");
            return (integer);
        }
      

        //---------------------------------------------------------------}
        // pop OBJECT Top of Stack
        public object PopObject()
        {
            object first = null;
			Debug.WriteLine("In PopObject()");
            if (stack.Count > 0)
            {
                first = stack.Pop();
				TraceInternal.TraceVerbose("PopObject: " + first.ToString());
            }
			Debug.WriteLine("Out PopObject()");
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
			
			Debug.WriteLine("In Add()");

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
			Debug.WriteLine("Out Add()");
        }

        //---------------------------------------------------------------}
        // SUBTRACT Top of Stack with Primary
        void Subtract()
        {
            object first;
            object second;
            double number;

            Debug.WriteLine("In Subtract()");

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
            Debug.WriteLine("Out Subtract()");
        }

        //---------------------------------------------------------------}
        // MULTIPLY Top of Stack with Primary
        void Multiply()
        {
            object first;
            object second;
            double numeric;

            Debug.WriteLine("In Multiply()");

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
            Debug.WriteLine("Out Multiply()");
        }

        //---------------------------------------------------------------}
        // DIVIDE Top of Stack with Primary
        void Divide()
        {
            object first;
            object second;
            double number;

            Debug.WriteLine("In Divide()");

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
            Debug.WriteLine("Out Divide()");
        }
      
        //---------------------------------------------------------------}
        // POWER Top of Stack with Primary
        private void Power()
        {
            object first;
            object second;
            double number;

            Debug.WriteLine("In Power()");

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
            Debug.WriteLine("Out Power()");
        }

        #endregion operators

        public int GetIntVariable(int varnum)
        {
            Debug.WriteLine("In GetIntVariable()");
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
            Debug.WriteLine("Out GetIntVariable()");
            return (integer);
        }

      
        public double GetNumericVariable(string varName)
        {
            double number;
            Debug.WriteLine("In GetNumericVariable()");
            if (numericVariables.ContainsKey(varName))
            {
                number = (double)numericVariables[varName];
            }
            else
            {
                number = 0;
            }
            TraceInternal.TraceVerbose("varName=" + varName + " number=" + number);
            Debug.WriteLine("Out GetNumericVariable()");
            return (number);
        }

        public double GetNumericArrayVariable(string varName, int positions, int[] position)
        {
            Debug.WriteLine("In GetNumericArrayVariable()");

            uBasicLibrary.Array data;
            double number;
            if (numericArrayVariables.ContainsKey(varName))
            {
                data = (uBasicLibrary.Array)numericArrayVariables[varName];
                number = (double)data.Get(position);
            }
            else
            {
                number = 0;
            }
            TraceInternal.TraceVerbose("varName=" + varName + " number=" + number);
            Debug.WriteLine("Out GetNumericArrayVariable()");
            return (number);
        }

        
        public void DeclareNumericArrayVariable(string varName, int dimensions, int[] dimension)
        {
            Debug.WriteLine("In DeclareNumericArrayVariable()");
            uBasicLibrary.Array data;
            if (numericArrayVariables.ContainsKey(varName))
            {
                Expected("Array already defined " + varName + "(");
            }
            data = new uBasicLibrary.Array(varName, dimensions, dimension,(double)0);
            numericArrayVariables.Add(varName, data);
            Debug.WriteLine("In DeclareNumericArrayVariable()");
        }

        

        public void SetIntVariable(int varnum, int integer)
        {
            Debug.WriteLine("In SetIntVariable()");
            if (varnum >= 0 && varnum <= MAX_VARNUM)
            {
                variables[varnum] = integer;
            }
            TraceInternal.TraceVerbose("varNum=" + varnum + " integer=" + integer);
            Debug.WriteLine("Out SetIntVariable()");
        }
     

        public void SetNumericVariable(string varName, double number)
        {
            Debug.WriteLine("In SetNumericVariable()");
            if (numericVariables.ContainsKey(varName))
            {
                numericVariables.Remove(varName);
            }
            numericVariables.Add(varName, number);
            TraceInternal.TraceVerbose("varName=" + varName + " number=" + number);
            Debug.WriteLine("Out SetNumericVariable()");
        }

        public void SetNumericArrayVariable(string varName, int positions, int[] position, double number)
        {
            Debug.WriteLine("In SetNumericArrayVariable()");
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
        
            TraceInternal.TraceVerbose("varName=" + varName + " number=" + number);
            Debug.WriteLine("Out SetNumericArrayVariable()");
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
