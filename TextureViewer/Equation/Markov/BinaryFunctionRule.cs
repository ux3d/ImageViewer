﻿using System.Collections.Generic;
using TextureViewer.Equation.Token;

namespace TextureViewer.Equation.Markov
{
    public class BinaryFunctionRule : MarkovRule
    {
        public BinaryFunctionRule()
        {
            Tokens = new List<Token.Token.Type>
            {
                Token.Token.Type.Function,
                 // 1st arg
                Token.Token.Type.Value,
                Token.Token.Type.Seperator,
                // 2nd arg
                Token.Token.Type.Value,
                Token.Token.Type.BracketClose
            };
        }

        public override List<Token.Token> Apply(List<Token.Token> match)
        {
            var function = (FunctionToken)match[0];
            var v1 = (ValueToken)match[1];
            var v2 = (ValueToken)match[3];
            return new List<Token.Token> {new BinaryFunctionToken(function.FuncName, v1, v2)};
        }
    }
}
