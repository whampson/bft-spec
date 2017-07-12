﻿#region License
/* Copyright (c) 2017 Wes Hampson
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
#endregion

using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace WHampson.Bft
{
    internal sealed class TypenameModifier : Modifier<string>
    {
        private static readonly Regex NameFormatRegex = new Regex(@"^[a-zA-Z_][\da-zA-Z_]*$");

        public TypenameModifier(XAttribute srcAttr)
            : base("typename", srcAttr)
        {
        }

        //public override string GetTryParseErrorMessage()
        //{
        //    return "'{0}' is not a valid variable name. Variable names can only consist of "
        //        + "alphanumeric characters and underscores, and cannot begin with a number.";
        //}

        public override bool TrySetValue(string valStr)
        {
            if (!NameFormatRegex.IsMatch(valStr))
            {
                return false;
            }

            Value = valStr;
            return true;
        }
    }
}
