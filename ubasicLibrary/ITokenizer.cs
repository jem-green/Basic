//  Copyright (c) 2017, Jeremy Green All rights reserved.

namespace ubasicLibrary
{
    public enum Token : int
    {
    }

    public interface ITokenizer
    {
        void AcceptToken(Token token);
        Token CheckSingleChar();
        Token GetNextToken();
        void GotoPosition(int position);
        void Init(int position);
        Token GetToken();
        void NextToken();
        void SkipTokens();
        int GetInteger();
        double GetNumber();
        string Getstring();
        bool IsFinished();
        int GetIntegerVariable();
        string GetNumericVariable();
        string GetNumericArrayVariable();
        string GetStringArrayVariable();
        string GetStringVariable();
        int GetPosition();
    }
}