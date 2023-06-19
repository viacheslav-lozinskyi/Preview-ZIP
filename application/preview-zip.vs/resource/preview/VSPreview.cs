using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace resource.preview
{
    internal class VSPreview : extension.AnyPreview
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

        protected override void _Execute(atom.Trace context, int level, string url, string file)
        {
            var a_Context = new Node();
            {
                __Execute(a_Context, ZipFile.OpenRead(file));
                __Execute(context, level - 1, a_Context, file);
            }
        }

        private static void __Execute(atom.Trace context, int level, Node data, string file)
        {
            if (GetState() == NAME.STATE.WORK.CANCEL)
            {
                return;
            }
            if (string.IsNullOrEmpty(data.Name) == false)
            {
                context.
                    SetComment(__GetComment(data), __GetHint(data)).
                    SetProgress(__GetType(data) == NAME.EVENT.FILE ?__GetProgress(data) : CONSTANT.PROGRESS.REMOVE, "[[[Compress Ratio]]]").
                    SetUrl(__GetUrl(data, file)).
                    Send(NAME.SOURCE.PREVIEW, __GetType(data), level, data.Name);
            }
            foreach (var a_Context in data.Children)
            {
                if (a_Context.IsFolder)
                {
                    __Execute(context, level + 1, a_Context, Path.Combine(file, a_Context.Name));
                }
            }
            foreach (var a_Context in data.Children)
            {
                if (a_Context.IsFolder == false)
                {
                    __Execute(context, level + 1, a_Context, Path.Combine(file, a_Context.Name));
                }
            }
        }

        private static void __Execute(Node data, ZipArchive file)
        {
            if (file != null)
            {
                foreach (var a_Context in file.Entries)
                {
                    __Execute(data, a_Context, a_Context.FullName);
                }
            }
        }

        private static void __Execute(Node data, ZipArchiveEntry file, string name)
        {
            if ((file != null) && (string.IsNullOrEmpty(name) == false))
            {
                var a_Index = name.IndexOf("\\");
                if (GetState() == NAME.STATE.WORK.CANCEL)
                {
                    return;
                }
                if (a_Index < 0)
                {
                    a_Index = name.IndexOf("/");
                }
                if (a_Index < 0)
                {
                    var a_Context = __GetNode(data, name);
                    {
                        a_Context.FullSize = (int)file.Length;
                        a_Context.CompressedSize = (int)file.CompressedLength;
                        a_Context.IsFolder = false;
                    }
                }
                else
                {
                    var a_Context = __GetNode(data, name.Substring(0, a_Index));
                    {
                        a_Context.Name = name.Substring(0, a_Index);
                        a_Context.IsFolder = true;
                    }
                    {
                        __Execute(a_Context, file, name.Substring(a_Index + 1, name.Length - a_Index - 1));
                    }
                }
            }
        }

        private static Node __GetNode(Node data, string name)
        {
            foreach (var a_Context in data.Children)
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
                    data.Children.Add(a_Result);
                }
                return a_Result;
            }
        }

        private static string __GetComment(Node data)
        {
            if (data.IsFolder == false)
            {
                if (data.FullSize > 0)
                {
                    return data.CompressedSize.ToString() + " / " + data.FullSize.ToString() + " / " + (100 - ((data.CompressedSize * 100) / data.FullSize)).ToString() + "%";
                }
                return data.FullSize.ToString() + " / 0%";
            }
            return "";
        }

        private static int __GetProgress(Node data)
        {
            if (data.IsFolder == false)
            {
                if (data.FullSize > 0)
                {
                    return 100 - ((data.CompressedSize * 100) / data.FullSize);
                }
            }
            return 0;
        }

        private static string __GetHint(Node data)
        {
            return data.IsFolder ? "" : "[[[Compressed size]]] / [[[Full size]]] / [[[Compress ratio]]]";
        }

        private static string __GetType(Node data)
        {
            return data.IsFolder ? NAME.EVENT.FOLDER : NAME.EVENT.FILE;
        }

        private static string __GetUrl(Node data, string url)
        {
            return data.IsFolder ? "" : url;
        }
    };
}
