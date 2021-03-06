﻿#region License
/* Copyright (c) 2017-2018 Wes Hampson
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
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using WHampson.Cascara.Extensions;

namespace WHampson.Cascara.Interpreter.Xml
{
    /// <summary>
    /// Represents a <see cref="Statement"/> in XML syntax.
    /// </summary>
    internal sealed class XmlStatement : Statement
    {
        /// <summary>
        /// Creates a new <see cref="XmlStatement"/> using the specified <see cref="XElement"/>.
        /// </summary>
        /// <param name="elem">The XML element to parse.</param>
        /// <returns>The new <see cref="XmlStatement"/> object.</returns>
        /// <exception cref="SyntaxException">
        /// Thrown if the specified <see cref="XElement"/> is malformatted
        /// (such as a text node being included inside the element).
        /// </exception>
        public static XmlStatement Parse(XElement elem)
        {
            XmlStatement stmt = new XmlStatement(elem);
            stmt.Parse();

            return stmt;
        }

        private XmlStatement(XElement sourceElement)
            : base(sourceElement.GetLineInfo())
        {
            if (sourceElement == null)
            {
                throw new ArgumentNullException(nameof(sourceElement));
            }

            Element = sourceElement;
        }

        /// <summary>
        /// Gets the <see cref="XElement"/> that created this <see cref="Statement"/>.
        /// </summary>
        internal XElement Element
        {
            get;
        }

        protected override void ExtractInfo()
        {
            // Extract keyword
            Keyword = Element.Name.LocalName;

            // Check syntax
            IEnumerable<XNode> textNodes = Element.Nodes()
                .Where(n => n.NodeType == XmlNodeType.Text);
            if (textNodes.Any())
            {
                string text = Element.Value.Trim().Ellipses(25);
                string fmt = Resources.SyntaxExceptionXmlUnexpectedText;
                XNode textNode = textNodes.ElementAt(0);
                ISourceEntity src = new XmlSourceEntity(textNode);
                throw LayoutScriptException.Create<SyntaxException>(null, src, fmt, text, Keyword);
            }

            // Extract parameters
            foreach (XAttribute attr in Element.Attributes())
            {
                SetParameter(attr.Name.LocalName, attr.Value);
            }

            // Parse nested statements
            foreach (XElement elem in Element.Elements())
            {
                AddNestedStatement(XmlStatement.Parse(elem));
            }
        }
    }
}
