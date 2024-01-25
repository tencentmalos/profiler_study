using System.Collections.Generic;
using System.Linq;

namespace SCLCoreCLR;

public class WildcardExpression
{
	private enum Operator
	{
		And,
		Or
	}

	private WildcardExpression m_Exp1;

	private Operator m_Operator;

	private WildcardExpression m_Exp2;

	private Wildcard m_Wildcard;

	public static bool Match(string value, string expression, ref string error)
	{
		return Match(value, expression, add_asterisks: false, ref error);
	}

	public static bool Match(string value, string expression, bool add_asterisks, ref string error)
	{
		return Parse(expression, add_asterisks, ref error).IsMatch(value);
	}

	private static bool IsWhiteSpace(char c)
	{
		if (c != ' ' && c != '\t' && c != '\r')
		{
			return c == '\n';
		}
		return true;
	}

	private static List<string> ParseTokens(string value)
	{
		List<string> list = new List<string>();
		int index = 0;
		string text = "";
		while (index < value.Length)
		{
			char c = value[index++];
			switch (c)
			{
			case '"':
			{
				string item = ParseString(value, ref index);
				list.Add(item);
				break;
			}
			case ' ':
				if (text != "")
				{
					list.Add(text);
				}
				text = "";
				break;
			case '(':
				if (text != "")
				{
					list.Add(text);
				}
				text = "";
				list.Add("(");
				break;
			case ')':
				if (text != "")
				{
					list.Add(text);
				}
				text = "";
				list.Add(")");
				break;
			default:
				text += c;
				break;
			}
		}
		if (text != "")
		{
			list.Add(text);
		}
		return list;
	}

	private static string ParseString(string value, ref int index)
	{
		string text = "";
		while (index < value.Length)
		{
			char c = value[index++];
			if (c == '"')
			{
				return text;
			}
			text += c;
		}
		return null;
	}

	public static WildcardExpression Parse(string value, ref string error)
	{
		return Parse(value, add_asterisks: false, ref error);
	}

	public static WildcardExpression Parse(string value, bool add_asterisks, ref string error)
	{
		List<string> tokens = ParseTokens(value);
		WildcardExpression wildcardExpression = new WildcardExpression();
		int index = 0;
		if (!ParseExpression(tokens, wildcardExpression, ref index, add_asterisks, ref error))
		{
			error = "error parsing string, defaulting to initial value";
			wildcardExpression.m_Wildcard = new Wildcard(value);
		}
		return wildcardExpression;
	}

	private static string AddAsterisks(string value)
	{
		if (!value.Contains('*'))
		{
			return "*" + value + "*";
		}
		return value;
	}

	private static bool ParseExpression(List<string> tokens, WildcardExpression expression, ref int index, bool add_asterisks, ref string error)
	{
		if (index == tokens.Count)
		{
			return true;
		}
		string text = tokens[index++];
		string text2 = text.ToLower();
		if (index == tokens.Count || tokens[index] == ")")
		{
			if (index != tokens.Count)
			{
				index++;
			}
			if (add_asterisks)
			{
				text = AddAsterisks(text);
			}
			expression.m_Wildcard = new Wildcard(text);
			return true;
		}
		WildcardExpression wildcardExpression = new WildcardExpression();
		switch (text)
		{
		case "(":
			if (!ParseExpression(tokens, wildcardExpression, ref index, add_asterisks, ref error))
			{
				return false;
			}
			break;
		default:
			if (!(text2 == "and") && !(text2 == "or"))
			{
				if (add_asterisks)
				{
					text = AddAsterisks(text);
				}
				wildcardExpression.m_Wildcard = new Wildcard(AddAsterisks(text));
				break;
			}
			goto case "&&";
		case "&&":
		case "||":
			error = "Unexpected conditional token";
			return false;
		}
		expression.m_Exp1 = wildcardExpression;
		if (index == tokens.Count)
		{
			return true;
		}
		text = tokens[index++];
		if (text == ")")
		{
			return true;
		}
		switch (text.ToLower())
		{
		case "&&":
		case "and":
			expression.m_Operator = Operator.And;
			break;
		case "||":
		case "or":
			expression.m_Operator = Operator.Or;
			break;
		default:
			error = "Unexpected token " + text;
			return false;
		}
		WildcardExpression wildcardExpression2 = new WildcardExpression();
		if (!ParseExpression(tokens, wildcardExpression2, ref index, add_asterisks, ref error))
		{
			return false;
		}
		expression.m_Exp2 = wildcardExpression2;
		return true;
	}

	public bool IsMatch(string value)
	{
		if (m_Wildcard != null)
		{
			return m_Wildcard.IsMatch(value);
		}
		bool flag = m_Exp1.IsMatch(value);
		if (flag && m_Operator == Operator.Or)
		{
			return true;
		}
		if (!flag && m_Operator == Operator.And)
		{
			return false;
		}
		if (m_Exp2 == null)
		{
			return true;
		}
		return m_Exp2.IsMatch(value);
	}

	public bool IsMatch(ICollection<string> values)
	{
		if (m_Wildcard != null)
		{
			foreach (string value in values)
			{
				if (m_Wildcard.IsMatch(value))
				{
					return true;
				}
			}
			return false;
		}
		bool flag = m_Exp1.IsMatch(values);
		if (flag && m_Operator == Operator.Or)
		{
			return true;
		}
		if (!flag && m_Operator == Operator.And)
		{
			return false;
		}
		if (m_Exp2 == null)
		{
			return true;
		}
		return m_Exp2.IsMatch(values);
	}
}
