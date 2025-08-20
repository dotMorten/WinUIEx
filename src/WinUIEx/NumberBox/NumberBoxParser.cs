using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Globalization.NumberFormatting;

namespace WinUIEx
{
    internal enum MathTokenType
    {
        Numeric,
        Operator,
        Parenthesis
    }

    internal sealed class MathToken
    {
        public MathTokenType Type { get; }
        public double Value { get; }      // used when Type == Numeric
        public char Char { get; }         // used when Type == Operator or Parenthesis

        public MathToken(MathTokenType type, double value)
        {
            Type = type;
            Value = value;
        }

        public MathToken(MathTokenType type, char ch)
        {
            Type = type;
            Char = ch;
        }
    }

    internal static class NumberBoxParser
    {
        private const string Operators = "+-*/^";

        // Returns list of MathTokens from expression input string. If there are any parsing errors, it returns an empty list.
        public static List<MathToken> GetTokens(string input, INumberParser numberParser)
        {
            var tokens = new List<MathToken>();
            if (input is null) return tokens;

            bool expectNumber = true;
            int i = 0;

            while (i < input.Length)
            {
                char nextChar = input[i];

                // Skip spaces
                if (nextChar != ' ')
                {
                    if (expectNumber)
                    {
                        if (nextChar == '(')
                        {
                            // Open paren allowed; doesn't change expectation
                            tokens.Add(new MathToken(MathTokenType.Parenthesis, nextChar));
                        }
                        else
                        {
                            // Try to parse a number starting at i
                            var segment = input.Substring(i);
                            var (value, charLength) = GetNextNumber(segment, numberParser);

                            if (charLength > 0)
                            {
                                tokens.Add(new MathToken(MathTokenType.Numeric, value));
                                i += charLength - 1; // advance to end of token (loop will +1)
                                expectNumber = false; // next should be operator
                            }
                            else
                            {
                                // Not a number where one was expected -> error
                                return new List<MathToken>();
                            }
                        }
                    }
                    else
                    {
                        if (Operators.IndexOf(nextChar) >= 0)
                        {
                            tokens.Add(new MathToken(MathTokenType.Operator, nextChar));
                            expectNumber = true; // next should be number (or '(')
                        }
                        else if (nextChar == ')')
                        {
                            // Close paren allowed; doesn't change expectation
                            tokens.Add(new MathToken(MathTokenType.Parenthesis, nextChar));
                        }
                        else
                        {
                            // Unrecognized where operator/close-paren expected
                            return new List<MathToken>();
                        }
                    }
                }

                i++;
            }

            return tokens;
        }

        // Attempts to parse a number from the beginning of the given input string.
        // Returns (value, matchedLength). If parsing fails, returns (NaN, 0).
        public static (double value, int matchedLength) GetNextNumber(string input, INumberParser numberParser)
        {
            // Attempt to parse anything before an operator or space as a number
            // Equivalent to L"^-?([^-+/*\\(\\)\\^\\s]+)"
            var regex = new Regex(@"^-?([^-+/*\(\)\^\s]+)", RegexOptions.CultureInvariant);
            var match = regex.Match(input);

            if (match.Success)
            {
                int len = match.Groups[0].Length;
                var parsed = numberParser.ParseDouble(input.Substring(0, len));
                if (parsed.HasValue)
                {
                    return (parsed.Value, len);
                }
            }

            return (double.NaN, 0);
        }

        private static int GetPrecedenceValue(char c)
        {
            if (c == '*' || c == '/') return 1;
            if (c == '^') return 2;
            return 0;
        }

        // Converts a list of tokens from infix format (e.g. "3 + 5") to postfix (e.g. "3 5 +")
        public static List<MathToken> ConvertInfixToPostfix(List<MathToken> infixTokens)
        {
            var postfixTokens = new List<MathToken>();
            var operatorStack = new Stack<MathToken>();

            foreach (var token in infixTokens)
            {
                if (token.Type == MathTokenType.Numeric)
                {
                    postfixTokens.Add(token);
                }
                else if (token.Type == MathTokenType.Operator)
                {
                    while (operatorStack.Count > 0)
                    {
                        var top = operatorStack.Peek();
                        if (top.Type != MathTokenType.Parenthesis &&
                            GetPrecedenceValue(top.Char) >= GetPrecedenceValue(token.Char))
                        {
                            postfixTokens.Add(operatorStack.Pop());
                        }
                        else
                        {
                            break;
                        }
                    }
                    operatorStack.Push(token);
                }
                else if (token.Type == MathTokenType.Parenthesis)
                {
                    if (token.Char == '(')
                    {
                        operatorStack.Push(token);
                    }
                    else
                    {
                        // Pop until matching '('
                        while (operatorStack.Count > 0 && operatorStack.Peek().Char != '(')
                        {
                            postfixTokens.Add(operatorStack.Pop());
                        }

                        if (operatorStack.Count == 0)
                        {
                            // Broken parenthesis
                            return new List<MathToken>();
                        }

                        // Discard the '('
                        operatorStack.Pop();
                    }
                }
            }

            // Pop all remaining operators
            while (operatorStack.Count > 0)
            {
                if (operatorStack.Peek().Type == MathTokenType.Parenthesis)
                {
                    // Broken parenthesis
                    return new List<MathToken>();
                }
                postfixTokens.Add(operatorStack.Pop());
            }

            return postfixTokens;
        }

        public static double? ComputePostfixExpression(List<MathToken> tokens)
        {
            var stack = new Stack<double>();

            foreach (var token in tokens)
            {
                if (token.Type == MathTokenType.Operator)
                {
                    // Need at least two operands
                    if (stack.Count < 2) return null;

                    double op1 = stack.Pop();
                    double op2 = stack.Pop();
                    double result;

                    switch (token.Char)
                    {
                        case '-':
                            result = op2 - op1;
                            break;
                        case '+':
                            result = op2 + op1;
                            break;
                        case '*':
                            result = op2 * op1;
                            break;
                        case '/':
                            if (op1 == 0)
                            {
                                // divide by zero
                                return double.NaN;
                            }
                            result = op2 / op1;
                            break;
                        case '^':
                            result = Math.Pow(op2, op1);
                            break;
                        default:
                            return null;
                    }

                    stack.Push(result);
                }
                else if (token.Type == MathTokenType.Numeric)
                {
                    stack.Push(token.Value);
                }
            }

            // Must end with exactly one value
            if (stack.Count != 1) return null;
            return stack.Peek();
        }

        public static double? Compute(string expr, INumberParser numberParser)
        {
            if (expr is null) return null;

            // Tokenize
            var tokens = GetTokens(expr, numberParser);
            if (tokens.Count == 0) return null;

            // Infix -> Postfix
            var postfix = ConvertInfixToPostfix(tokens);
            if (postfix.Count == 0) return null;

            // Evaluate
            return ComputePostfixExpression(postfix);
        }
    }
}
