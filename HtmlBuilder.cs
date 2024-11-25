using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Dolores.Utility
{
    using Extension;
    /* Node+Node => NodeList
     * NodeList+Node => NodeList
     * Tag/Node => Node is child of Tag
     * Tag/NodeList => Nodes in NodeList is child of Tag
     * 
     */
    public class HtmlBuilder {
        public static HtmlBuilder Begin(Func<NodeBuilder, Node> f) {
            return new HtmlBuilder(f(new NodeBuilder()));
        }

        public HtmlBuilder(Node n) { _n = n; }

        Node _n;
        bool _indent = false;

        public HtmlBuilder indent(bool b) { _indent = b; return this; }

        public override string ToString() {
            var sb = new StringBuilder();
            _n.Emit(sb, _indent, 0, "\t");
            return sb.ToString();
        }

        public class NodeBuilder {
            public Tag html     { get { return new Tag("html" ); } }
            public Tag head     { get { return new Tag("head" ); } }
            public Tag body     { get { return new Tag("body" ); } }
            public Tag p        { get { return new Tag("p"    ); } }
            public Tag title    { get { return new Tag("title"); } }
            public Tag h1       { get { return new Tag("h1"   ); } }
            public Tag h2       { get { return new Tag("h2"   ); } }
            public Tag ul       { get { return new Tag("ul"   ); } }
            public Tag li       { get { return new Tag("li"   ); } }
            public Tag div      { get { return new Tag("div"  ); } }
            public Tag pre      { get { return new Tag("pre"  ); } }
            public Tag meta(string name, string content) {
                return new Tag("meta").attr("name", name).attr("content", content);
            }
            public TextNode text(string content) {
                return new TextNode(content);
            }
            public HtmlFragment htmlfragment(string content) {
                return new HtmlFragment(content);
            }
            public T code<T>(Func<T> block) where T :Node {
                return block();
            }
            public IEnumerable<T> code<T>(Func<IEnumerable<T>> block) where T :Node {
                return block();
            }
            public IEnumerable<Node> code(Func<NodeList> block) {
                return block();
            } //oops
			public Tag container {
                get {
                    return new Tag(string.Empty);
                }
            }
        }
        public abstract class Node {
            public abstract void Emit(StringBuilder sb, bool oneline, int indent_level, string indent_string);
            public abstract bool IsSingular();
            public static NodeList operator +(Node n1, Node n2) {
                var nl = new NodeList();
                nl.Add(n1);
                nl.Add(n2);
                return nl;
            }
            public static Node operator +(Node node) { //unary + : do nothing but code looks better
                return node;
            }
        }
        //binary operator(lhs,rhs) is must defined in type definition of lhs or rhs. so operator(IList<Node>,IEnumerable<Node>) can't define.
        public class NodeList :List<Node> {
            public NodeList() { }
            public static NodeList operator +(NodeList nodes, Node n) {
                nodes.Add(n);
                return nodes;
            }
            public static NodeList operator +(NodeList nodes, IEnumerable<Node> rest) {
                nodes.AddRange(rest);
                return nodes;
            }
        }
        public class HtmlFragment :Node {
            public HtmlFragment(string content) {
                _content = content;
            }
            protected string _content;
            public override void Emit(StringBuilder sb, bool oneline, int indent_level, string indent_string) {
                sb.Append(_content);
            }
            public override bool IsSingular() { return true; }
        }
        public class TextNode :HtmlFragment {
            public TextNode(string content) : base(content) { }
            public override string ToString() {
                return "Text node: " + _content;
            }
            public override void Emit(StringBuilder sb, bool oneline, int indent_level, string indent_string) {
                sb.Append(HttpUtility.HtmlEncode(_content));
            }
        }
        public class Tag :Node {
            public Tag(string name) { _name = name; }
            public Tag attr_id(string name) { return attr("id", name); }
            public Tag attr_class(params string[] name) { return attr("class", name.Join(' ')); }
            public Tag attr(string name, string value) {
                _attr = _attr + " " + HttpUtility.HtmlEncode(name) + "=\"" + HttpUtility.HtmlEncode(value) + "\"";
                return this;
            }
            public Tag this[object text_node_content] {
                get { AppendChild(new TextNode(text_node_content.ToString())); return this; }
            }

            public void AppendChild(Node n) {
                if(_last_appended_target != null)
                    _last_appended_target.AppendChild(n);
                else
                    _children.Add(n);
            }
            // AppendChild(Node) and AppendChild(Tag): both methods is looks similar, but we should distinguish these carefully.
            public void AppendChild(Tag t) {
                if(_last_appended_target != null)
                    _last_appended_target.AppendChild(t);
                else
                    _children.Add(t);
                _last_appended_target = t;
            }
            // ** NOTE **
            // Expression tag1/tag2/tag3 makes [tag1 [tag2 [tag3]]], it is clear.
            // But tag1/(tag2+tag3)/tag4 makes [tag1 [tag2 tag3 tag4]].
            // It seems very weird. i wish it produce compile-time error, but have not any idea.
            public void AppendChild<T>(IEnumerable<T> ns) where T :Node { _children.AddRange(ns.Cast<Node>()); }

            public override void Emit(StringBuilder sb, bool format, int indent_level, string indent_string) {
                if(format) sb.Append(indent_string.Repeat(indent_level));
                sb.Append('<');
                sb.Append(_name);
                if(_attr.Length > 0) {
                    sb.Append(_attr);
                }
                if(_children.IsNullOrEmpty()) {
                    sb.Append(" />");
                } else {
                    bool oneline = !format || this.IsSingular();
                    sb.Append('>');
                    if(!oneline)
                        sb.AppendLine();
                    foreach(var child in _children) {
                        child.Emit(sb, !oneline, indent_level + 1, indent_string);
                        if(!oneline) sb.AppendLine();
                    }
                    if(!oneline) sb.Append(indent_string.Repeat(indent_level));
                    sb.Append("</");
                    sb.Append(_name);
                    sb.Append('>');
                }
            }
            public override bool IsSingular() {
                return _children.IsNullOrEmpty() || (_children.Count == 1 && _children.First().IsSingular());
            }
            public override string ToString() {
                return "Tag: " + _name + "(children=" + _children.Count + ")";
            }

            string _name;
            string _attr = "";
            protected List<Node> _children = new List<Node>(); //TODO: delay initialize
            Tag _last_appended_target = null;

            public static Tag operator /(Tag parent, Node child) {
                parent.AppendChild(child);
                return parent;
            }
            public static Tag operator /(Tag parent, Tag child) {
                parent.AppendChild(child);
                return parent;
            }
            public static Tag operator /(Tag parent, IEnumerable<Node> children) {
                parent.AppendChild(children);
                return parent;
            }
            public static Tag operator /(Tag parent, IEnumerable<Tag> children) {
                parent.AppendChild(children);
                return parent;
            }
            public static Tag operator +(Tag t) { return t; }
        }
    }
}
