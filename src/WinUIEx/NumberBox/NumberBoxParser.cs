using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        public double? Value { get; }      // used when Type == Numeric
        public char Char { get; }         // used when Type == Operator or Parenthesis

        public MathToken(double value)
        {
            Type = MathTokenType.Numeric;
            Value = value;
        }

        public MathToken(MathTokenType type, char ch)
        {
            Type = type;
            Char = ch;
        }
    }

    internal static class NumberBoxParser<T> where T : System.Numerics.INumber<T>
    {
        private const string Operators = "+-*/^";

        // Returns list of MathTokens from expression input string. If there are any parsing errors, it returns an empty list.
        public static List<MathToken> GetTokens(string input, INumberParser numberParser, CultureInfo? culture)
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
                            var (value, charLength) = GetNextNumber(segment, numberParser, culture);

                            if (charLength > 0)
                            {
                                tokens.Add(new MathToken(value));
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
        // Returns (value, matchedLength). If parsing fails, returns (0, -1).
        public static (double value, int matchedLength) GetNextNumber(string input, INumberParser numberParser, CultureInfo? culture)
        {
            // Attempt to parse anything before an operator or space as a number
            // Equivalent to L"^-?([^-+/*\\(\\)\\^\\s]+)"
            var regex = new Regex(@"^-?([^-+/*\(\)\^\s]+)", RegexOptions.CultureInvariant);
            var match = regex.Match(input);

            if (match.Success)
            {
                int len = match.Groups[0].Length;
                var text = input.Substring(0, len);
                // Try just using normal parsing from INumber<T> first if using Decimals.
                var parsed = numberParser.ParseDouble(text);
                if (parsed.HasValue)
                {
                    return (parsed.Value, len);
                }
                if (double.TryParse(text, culture, out double result))
                {
                    return (result, len);
                }
            }

            return (double.NaN, -1);
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

        public static T ComputePostfixExpression(List<MathToken> tokens, out bool success)
        {
            success = false;
            var stack = new Stack<double>();

            foreach (var token in tokens)
            {
                if (token.Type == MathTokenType.Operator)
                {
                    // Need at least two operands
                    if (stack.Count < 2) return T.Zero;

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
                            if (op1 == 0d)
                            {
                                // divide by zero
                                return T.Zero;
                            }
                            result = op2 / op1;
                            break;
                        case '^':
                            result = Math.Pow(op2, op1);
                            break;
                        default:
                            return T.Zero;
                    }

                    stack.Push(result);
                }
                else if (token.Type == MathTokenType.Numeric)
                {
                    stack.Push(token.Value!.Value);
                }
            }

            // Must end with exactly one value
            if (stack.Count != 1) return T.Zero;
            success = true;
            return T.CreateSaturating(stack.Peek());
        }

        public static T Compute(string expr, INumberParser numberParser, CultureInfo? culture, out bool success)
        {
            success = false;
            if (expr is null) return T.Zero;

            // Tokenize
            var tokens = GetTokens(expr, numberParser, culture);
            if (tokens.Count == 0) return T.Zero;

            // Infix -> Postfix
            var postfix = ConvertInfixToPostfix(tokens);
            if (postfix.Count == 0) return T.Zero;

            // Evaluate
            var result = ComputePostfixExpression(postfix, out success);
            return result;
        }
    }
}
