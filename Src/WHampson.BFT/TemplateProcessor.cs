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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using WHampson.BFT.Types;
using static WHampson.BFT.Keyword;

using Int32 = WHampson.BFT.Types.Int32;

namespace WHampson.BFT
{
    internal class TemplateProcessor
    {
        //private const string VariableRegex = "\\$\\{(.+)\\}";

        // Things to remember
        //  * Ensure that custom types have a fixed size

        private XDocument doc;
        private SyntaxParser syntaxParser;
        private Dictionary<string, CustomTypeInfo> customTypeMap;

        public TemplateProcessor(XDocument doc)
        {
            this.doc = doc;
            syntaxParser = new SyntaxParser(doc);
            customTypeMap = new Dictionary<string, CustomTypeInfo>();
        }

        public void Preprocess()
        {
            syntaxParser.ParseTemplateStructure();
            customTypeMap = syntaxParser.CustomTypeMap;
            //Console.WriteLine(SizeOf(tree.Root));
            int offset = 0;
            PrintNode(doc.Root, 0, ref offset);
        }

        public void PrintNode(XElement e, int depth, ref int offset)
        {
            string identifier = e.Name.LocalName;
            if (identifier == Keyword.TypedefIdentifier)
            {
                return;
            }

            IEnumerable<XElement> children = e.Elements();

            CustomTypeInfo customType;
            BuiltinType kw;
            bool isCustomType = customTypeMap.TryGetValue(identifier, out customType);
            if (isCustomType)
            {
                children = new List<XElement>(customType.Members);
                identifier = customType.Kind.ToString().ToLower();
                kw = customType.Kind;
            }
            else if (identifier == "bft")
            {
                kw = BuiltinType.Struct;
            }
            else if (identifier == AlignIdentifier)
            {
                XAttribute countAttr = e.Attribute(CountIdentifier);
                XAttribute kindAttr = e.Attribute(KindIdentifier);

                BuiltinType kind;
                if (kindAttr == null || !BuiltinTypeIdentifierMap.TryGetValue(kindAttr.Value, out kind))
                {
                    kind = BuiltinType.Int8;
                }

                int count = 1;
                int.TryParse(countAttr.Value, out count);

                Type t = TypeMap[kind];
                offset += count * (Marshal.SizeOf(t));

                return;
            }
            else
            {
                kw = BuiltinTypeIdentifierMap[identifier];
            }

            Console.WriteLine("{0:X4}: {1}{2}", offset, new string(' ', 2 * depth), identifier);

            if (kw != BuiltinType.Struct)
            {
                Type t = TypeMap[kw];
                offset += Marshal.SizeOf(t);
            }

            foreach (XElement child in children)
            {
                PrintNode(child, depth + 1, ref offset);
            }
        }

        //private void PrintNode(ISyntaxTreeNode n, int offset)
        //{
        //    Console.Write("{0:D4}: ", offset);
        //    Console.WriteLine(n);
        //    foreach (ISyntaxTreeNode child in n.Children)
        //    {
        //        PrintNode(child, offset);
        //        offset += SizeOf(child);
        //    }
        //}

        //private int SizeOf(ISyntaxTreeNode n)
        //{
        //    int size = 0;
        //    if (n is DataTypeTreeNode)
        //    {
        //        DataTypeTreeNode dn = (DataTypeTreeNode) n;
        //        if (dn.Type == BuiltinType.Struct)
        //        {
        //            foreach  (ISyntaxTreeNode memb in dn.Children)
        //            {
        //                size += SizeOf(memb);
        //            }
        //        }
        //        else
        //        {
        //            size = Marshal.SizeOf(TypeMap[dn.Type]);
        //        }
        //        string countStr;
        //        n.Modifiers.TryGetValue(Modifier.Count, out countStr);
        //        int count;
        //        bool result = int.TryParse(countStr, out count);
        //        if (!result)
        //        {
        //            count = 1;
        //        }

        //        size *= count;
        //    }
        //    else if (n is DirectiveTreeNode)
        //    {
        //        string countStr;
        //        n.Modifiers.TryGetValue(Modifier.Count, out countStr);
        //        int count;
        //        bool result = int.TryParse(countStr, out count);
        //        if (!result)
        //        {
        //            count = 1;
        //        }

        //        string kindStr;
        //        if (!n.Modifiers.TryGetValue(Modifier.Kind, out kindStr))
        //        {
        //            kindStr = Int8Identifier;
        //        }

        //        // TODO: handle typedef'd types
        //        BuiltinType type;
        //        if (!BuiltinTypeIdentifierMap.TryGetValue(kindStr, out type))
        //        {
        //            throw new TemplateException(string.Format("Unknown type '{0}'", kindStr));
        //        }

        //        int typeSize = size = Marshal.SizeOf(TypeMap[type]);
        //        size = count * typeSize;
        //    }

        //    return size;
        //}

        //private int SizeOf(XElement data)
        //{
        //    BuiltinType kind;
        //    IEnumerable<XElement> members = data.Descendants();

        //    string typeName = data.Name.LocalName;

        //    bool found = BuiltinTypeIdentifierMap.TryGetValue(typeName, out kind);
        //    if (!found)
        //    {
        //        CustomTypeInfo info;
        //        customTypeMap.TryGetValue(typeName, out info);
        //        kind = info.Kind;
        //        members = info.Members;
        //    }

        //    int siz = 0;
        //    if (kind == BuiltinType.Struct)
        //    {
        //        foreach (XElement memb in members)
        //        {
        //            siz += SizeOf(memb);
        //        }
        //    }
        //    else
        //    {
        //        Type t = TypeMap[kind];
        //        siz = Marshal.SizeOf(t);
        //    }

        //    return siz;
        //}

        internal static Dictionary<BuiltinType, Type> TypeMap = new Dictionary<BuiltinType, Type>()
        {
            { BuiltinType.Float, typeof(Float) },
            { BuiltinType.Int8, typeof(Int8) },
            { BuiltinType.Int32, typeof(Int32) }
        };

    }
}
