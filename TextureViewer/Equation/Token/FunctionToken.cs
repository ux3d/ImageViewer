﻿namespace TextureViewer.Equation.Token
{
    public class FunctionToken : Token
    {
        public readonly string FuncName;

        public FunctionToken(string name) : 
            base(Type.Function)
        {
            this.FuncName = name;
        }
    }
}
