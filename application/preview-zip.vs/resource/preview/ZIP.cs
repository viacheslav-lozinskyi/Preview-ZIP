
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace resource.preview
{
    public class ZIP : cartridge.AnyPreview
    {
        private class Node
        {
            public string Name { get; set; }
            public int FullSize { get; set; }
            public int CompressedSize { get; set; }
            public bool IsFolder { get; set; }
            public List<Node> Children { get { return m_Children; } }

            private List<Node> m_Children = new List<Node>();
        }

        protected override void _Execute(atom.Trace context, string url)
        {
            var a_Context = new Node();
            {
                __Execute(a_Context, ZipFile.OpenRead(url));
                __Execute(a_Context, 0, context, url);
            }
        }

        private static void __Execute(Node node, ZipArchive data)
        {
            if (data != null)
            {
                foreach (var a_Context in data.Entries)
                {
                    __Execute(node, a_Context, a_Context.FullName);
                }
            }
        }

        private static void __Execute(Node node, ZipArchiveEntry data, string name)
        {
            if ((data != null) && (string.IsNullOrEmpty(name) == false))
            {
                var a_Index = name.IndexOf("\\");
                if (a_Index < 0)
                {
                    a_Index = name.IndexOf("/");
                }
                if (a_Index < 0)
                {
                    var a_Context = __GetNode(node, name);
                    {
                        a_Context.FullSize = (int)data.Length;
                        a_Context.CompressedSize = (int)data.CompressedLength;
                        a_Context.IsFolder = false;
                    }
                }
                else
                {
                    var a_Context = __GetNode(node, name.Substring(0, a_Index));
                    {
                        a_Context.Name = name.Substring(0, a_Index);
                        a_Context.IsFolder = true;
                    }
                    {
                        __Execute(a_Context, data, name.Substring(a_Index + 1, name.Length - a_Index - 1));
                    }
                }
            }
        }

        private static void __Execute(Node node, int level, atom.Trace context, string url)
        {
            if (string.IsNullOrEmpty(node.Name) == false)
            {
                context.
                    Clear().
                    SetContent(node.Name).
                    SetComment(__GetComment(node)).
                    SetHint(__GetHint(node)).
                    SetPattern(__GetPattern(node)).
                    SetUrl(__GetUrl(node, url)).
                    SetLevel(level).
                    Send();
            }
            foreach (var a_Context in node.Children)
            {
                if (a_Context.IsFolder)
                {
                    __Execute(a_Context, level + 1, context, Path.Combine(url, a_Context.Name));
                }
            }
            foreach (var a_Context in node.Children)
            {
                if (a_Context.IsFolder == false)
                {
                    __Execute(a_Context, level + 1, context, Path.Combine(url, a_Context.Name));
                }
            }
        }

        private static Node __GetNode(Node node, string name)
        {
            foreach (var a_Context in node.Children)
            {
                if (a_Context.Name.ToUpper() == name.ToUpper())
                {
                    return a_Context;
                }
            }
            {
                var a_Result = new Node();
                {
                    a_Result.Name = name;
                    a_Result.FullSize = 0;
                    a_Result.CompressedSize = 0;
                    a_Result.IsFolder = false;
                }
                {
                    node.Children.Add(a_Result);
                }
                return a_Result;
            }
        }

        private static string __GetComment(Node node)
        {
            if (node.IsFolder == false)
            {
                if (node.FullSize > 0)
                {
                    return node.CompressedSize.ToString() + " / " + node.FullSize.ToString() + " / " + (100 - ((node.CompressedSize * 100) / node.FullSize)).ToString() + "%";
                }
                return node.FullSize.ToString() + " / 0%";
            }
            return "";
        }

        private static string __GetHint(Node node)
        {
            return node.IsFolder ? "" : "[[Compressed size]] / [[Full size]] / [[Compress ratio]]";
        }

        private static string __GetPattern(Node node)
        {
            return node.IsFolder ? NAME.PATTERN.FOLDER : /*NAME.PATTERN.ELEMENT*/"";
        }

        private static string __GetUrl(Node node, string url)
        {
            return node.IsFolder ? "" : url;
        }
    };
}
