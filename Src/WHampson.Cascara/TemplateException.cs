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

using System;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace WHampson.Cascara
{
    public class TemplateException : Exception
    {
        public TemplateException()
            : base()
        {
        }

        public TemplateException(string message)
            : base(message)
        {
        }

        public TemplateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected TemplateException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        internal static TemplateException Create(XObject obj, string msg)
        {
            msg = AppendLineInfo(obj, msg);
            return new TemplateException(msg);
        }

        internal static TemplateException Create(XObject obj, string msgFmt, params object[] fmtArgs)
        {
            string msg = string.Format(msgFmt, fmtArgs);
            msg = AppendLineInfo(obj, msg);

            return new TemplateException(msg);
        }

        internal static TemplateException Create(Exception innerException, XObject obj, string msg)
        {
            msg = AppendLineInfo(obj, msg);
            return new TemplateException(msg, innerException);
        }

        internal static TemplateException Create(Exception innerException, XObject obj, string msgFmt, params object[] fmtArgs)
        {
            string msg = string.Format(msgFmt, fmtArgs);
            msg = AppendLineInfo(obj, msg);

            return new TemplateException(msg, innerException);
        }

        private static string AppendLineInfo(XObject obj, string msg)
        {
            IXmlLineInfo lineInfo = obj;
            if (!msg.EndsWith("."))
            {
                msg += ".";
            }
            msg += string.Format(" Line {0}, position {1}.", lineInfo.LineNumber, lineInfo.LinePosition);

            return msg;
        }
    }
}