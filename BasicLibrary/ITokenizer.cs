//  Copyright (c) 2017, Jeremy Green All rights reserved.

namespace uBasicLibrary
{
    public interface ITokenizer
    {
        void AcceptToken(Tokenizer.Token token);
        Tokenizer.Token CheckSingleChar();
        Tokenizer.Token GetNextToken();
        void GotoPosition(int position);
        void Init(int position);
        Tokenizer.Token GetToken();
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