using System.Text;

namespace Drizzle.Lingo.Runtime.Parser;

public static class DebugPrint
{
    public static string PrintAstNode(AstNode.Base node)
    {
        var sb = new StringBuilder();

        node.DebugPrint(sb, 0);

        return sb.ToString();
    }
}