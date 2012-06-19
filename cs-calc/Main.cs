//<%@ WebService Language="C#" Class="cscalc.MainService" %>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.Services;
using System.Globalization;

namespace CsCalc
{
	public enum Token
	{
		Number,
		Operator,
		OpenParanthesis,
		CloseParanthesis,
		Word
	}
	
	public enum CharType
	{
		Whitespace,
		Number,
		Letter,
		Operator,
		OpenParanthesis,
		CloseParanthesis,
		EOF,
		NULL
	}
	
	/// <summary>
	/// Defines current state of lexer for processing next input.
	/// </summary>
	delegate State State(Lexer l);
	
	/// <summary>
	/// Interface to implement for handling emitted tokens of Lexer instance.
	/// </summary>
	interface ITokenReceiver
	{
		void ReceiveToken(Token token, string lexeme);
	}
	
	/// <summary>
	/// Simple receiver that prints token to console.
	/// </summary>
	class PrinterReceiver : ITokenReceiver
	{
		public void ReceiveToken(Token token, string lexeme)
		{
			Console.WriteLine("received token: {0}, {1}", token, lexeme);
		}
	}
	
	/// <summary>
	/// Lexer, Tokenizes input.
	/// </summary>
	/// <exception cref='Exception'>
	/// Represents errors that occur during application execution.
	/// </exception>
	class Lexer
	{
		public const char EOF = '\0';
		
		ITokenReceiver receiver;
		string input;
		
		State state;
		int pos;
		int start;
		
		public Lexer(ITokenReceiver receiver, string input)
		{
			this.input = input;
			this.receiver = receiver;
			
			state = lexWhitespace;
		}
		
		public void Run()
		{
			while (state != null)
			{
				state = state(this);
			}
		}
		
		void emit(Token token)
		{
			receiver.ReceiveToken(token, input.Substring(start, pos-start));
		}
		
		void reset()
		{
			start = pos;
		}
		
		void next()
		{
			pos++;
		}
		
		void discard()
		{
			next();
			reset();
		}
		
		char read()
		{
			if (pos >= input.Length)
			{
				return EOF;
			}
			return input[pos];
		}
		
		/// <summary>
		/// Gets the CharType of a char, providing a means for the
		/// State delegate to maintain switch/case for readability
		/// and easier debugging once this program becomes
		/// indeterminate.
		/// </summary>
		/// <returns>
		/// The CharType of c.
		/// </returns>
		/// <param name='c'>
		/// C.
		/// </param>
		static CharType getType(char c)
		{
			if (Char.IsWhiteSpace(c))
			{
				return CharType.Whitespace;
			}
			else if (Char.IsLetter(c))
			{
				return CharType.Letter;
			}
			else if (Char.IsNumber(c) || c == '.')
			{
				return CharType.Number;
			}
			else if (c == '(')
			{
				return CharType.OpenParanthesis;
			}
			else if (c == ')')
			{
				return CharType.CloseParanthesis;
			}
			else if (c == '+' || c == '-' || c == '*' || c == '/' || c == '^')
			{
				return CharType.Operator;
			}
			else if (c == EOF)
			{
				return CharType.EOF;
			}
			else
			{
				return CharType.NULL;
			}
		}
		
		static State lexWhitespace(Lexer l)
		{
			switch (getType(l.read()))
			{
			case CharType.Whitespace:
				l.discard();
				return lexWhitespace;
			case CharType.OpenParanthesis:
				l.next();
				l.emit(Token.OpenParanthesis);
				l.reset();
				return lexWhitespace;
			case CharType.CloseParanthesis:
				l.next();
				l.emit(Token.CloseParanthesis);
				l.reset();
				return lexWhitespace;
			case CharType.Number:
				l.reset();
				return lexNumber;
			case CharType.Operator:
				l.reset();
				return lexOperator;
			case CharType.Letter:
				l.reset();
				return lexWord;
			case CharType.EOF:
				return null;
			default:
				throw new Exception("Invalid input.");
			}
		}
		
		static State lexNumber(Lexer l)
		{
			switch (getType(l.read()))
			{
			case CharType.Number:
				l.next();
				return lexNumber;
			case CharType.EOF:
				l.emit(Token.Number);
				return null;
			default:
				l.emit(Token.Number);
				l.reset();
				return lexWhitespace;
			}
		}
		
		static State lexOperator(Lexer l)
		{
			switch (getType(l.read()))
			{
			case CharType.Operator:
				l.next();
				return lexOperator;
			case CharType.EOF:
				l.emit(Token.Operator);
				return null;
			default:
				l.emit(Token.Operator);
				l.reset();
				return lexWhitespace;
			}
		}
		
		static State lexWord(Lexer l)
		{
			switch (getType(l.read()))
			{
			case CharType.Letter:
				l.next();
				return lexWord;
			case CharType.EOF:
				l.emit(Token.Word);
				return null;
			default:
				l.emit(Token.Word);
				l.reset();
				return lexWhitespace;
			}
		}
	}
	
	/// <summary>
	/// Parser implementation that converts infix to postfix notation and evaluates.
	/// </summary>
	/// <exception cref='Exception'>
	/// Represents errors that occur during application execution.
	/// </exception>
	class Parser : ITokenReceiver
	{
		Dictionary<string, decimal> dict;
		List<string> wordNumBuf;
		
		// handles retaining parsed decimals from input and string operators
		// ordered in postfix notation for later evaluation.
		ArrayList postfix;
		
		// this stack is used during Lexer.run with Parser.ReceiveToken
		// as well as during evaluate. It should be empty after each
		// invocation is complete. During it's course, it will hold
		// strings and decimals. Due to the ordering of access, it should
		// always be known what type is expected. TODO might be clearer
		// to provide two separate containers for each method invocation.
		Stack stack;
		
		ITokenReceiver printer;
		bool debug = true;
		
		public Parser()
		{
			postfix = new ArrayList();
			stack = new Stack();
			printer = new PrinterReceiver();
			wordNumBuf = new List<string>();
			
			dict = new Dictionary<string, decimal>();
			dict.Add("zero", 0);
			dict.Add("one", 1);
			dict.Add("two", 2);
			dict.Add("three", 3);
			dict.Add("four", 4);
			dict.Add("five", 5);
			dict.Add("six", 6);
			dict.Add("seven", 7);
			dict.Add("eight", 8);
			dict.Add("nine", 9);
			dict.Add("ten", 10);
			dict.Add("eleven", 11);
			dict.Add("twelve", 12);
			dict.Add("thirteen", 13);
			dict.Add("fourteen", 14);
			dict.Add("fifteen", 15);
			dict.Add("sixteen", 16);
			dict.Add("seventeen", 17);
			dict.Add("eighteen", 18);
			dict.Add("nineteen", 19);
			dict.Add("twenty", 20);
			dict.Add("thirty", 30);
			dict.Add("fourty", 40); // I cant stop typing fourty
			dict.Add("forty", 40);
			dict.Add("fifty", 50);
			dict.Add("sixty", 60);
			dict.Add("seventy", 70);
			dict.Add("eighty", 80);
			dict.Add("ninety", 90);
			dict.Add("hundred", 100);
			dict.Add("thousand", 1000);
			dict.Add("million", 1000000);
			dict.Add("billion", 1000000000);
			dict.Add("trillion", 1000000000000);
			dict.Add("quadrillion", 1000000000000000);
			dict.Add("quintillion", 1000000000000000000);
		}
		
		public decimal Parse(string input)
		{
			Lexer lexer = new Lexer(this, input);
			lexer.Run();
			flushWordNumBuf();
			while (stack.Count != 0)
			{
				postfix.Add(stack.Pop());
			}
			return evaluate();
		}
		
		
		public void ReceiveToken(Token token, string lexeme)
		{
			
			if (debug) { printer.ReceiveToken(token, lexeme); }
			
			// TODO this is ugly
			if (token == Token.Word)
			{
				Token wordTokenResult = handleWord(lexeme);
				if (wordTokenResult == Token.Operator)
				{
					flushWordNumBuf();
				}
				return;
			}
			// else is here so I dont forget this all ties together when i (hopefully) refactor
			else
			{
				flushWordNumBuf();
			}
			
			//
			string s;
			
			switch (token)
			{
			case Token.Number:
				postfix.Add(decimal.Parse(lexeme));
				break;
			case Token.OpenParanthesis:
				stack.Push(lexeme);
				break;
			case Token.CloseParanthesis:
				while ((s = (string) stack.Pop()) != "(")
				{
					postfix.Add(s);
				}
				break;
			case Token.Operator:
				if (stack.Count == 0)
				{
					stack.Push(lexeme);
				}
				else
				{
					s = (string) stack.Peek();
					if (getPrecedence(s) > getPrecedence(lexeme))
					{
						while (stack.Count != 0)
						{
							postfix.Add(stack.Pop());
						}
					}
					stack.Push(lexeme);
				}
				break;
			}
		}
		
		/// <summary>
		/// Handles the word, emitting a relevant token for processing.
		/// </summary>
		/// <returns>
		/// The token type. This provides a pseudo look-ahead of sorts for this parser's ReceiveToken method
		/// for accumulating number phrases such as "two hundred" and knowing when to handle those phrases.
		/// </returns>
		/// <param name='word'>
		/// Word.
		/// </param>
		Token handleWord(string word)
		{
			// handle operators
			if (word.Equals("plus") || word.Equals("add") || word.Equals("added"))
			{
				ReceiveToken(Token.Operator, "+");
				return Token.Operator;
			}
			else if (word.Equals("minus") || word.Equals("subtract") || word.Equals("subtracted"))
			{
				ReceiveToken(Token.Operator, "-");
				return Token.Operator;
			}
			else if (word.Equals("divide") || word.Equals("divided"))
			{
				ReceiveToken(Token.Operator, "/");
				return Token.Operator;
			}
			else if (word.Equals("times") || word.Equals("multiplied"))
			{
				ReceiveToken(Token.Operator, "*");
				return Token.Operator;
			}
			else if (dict.ContainsKey(word))
			{
				wordNumBuf.Add(word);
				return Token.Number;
			}
			
			return Token.Word;
		}
		
		/// <summary>
		/// Flushs the word number buffer. With the following assumptions in mind, words can be separated into a set of tuples
		/// with length of two. The contents of each tuple is multipled and the results of each tuple operation is added. For
		/// example, "two hundred and three". "and" would be discarded by the parser leaving "two hundred three". This would result
		/// in the set [("two", "hundred"), ("three",)] thus resulting in the expression (2 * 100) + (3). There is one exception to
		/// this rule, words that end in "ty", of which tuple contents should be added. The implementation here forgoes the above
		/// structuring.
		/// </summary>
		void flushWordNumBuf()
		{
			if (wordNumBuf.Count == 0) { return; }
			
			decimal result = 0;
			decimal prev = 0;
			
			for (int i = 0; i < wordNumBuf.Count; i++)
			{
				string word = wordNumBuf[i];
				
				if ((i % 2) != 0)
				{
					result += (prev * dict[word]);
				}
				else if (word.EndsWith("ty"))
				{
					// HACK takes advantage of the above operation to handle "-ty" numbers
					// as noted in the method comments.
					prev = 1;
					result += dict[word];
				}
				else
				{
					prev = dict[wordNumBuf[i]];
				}
			}
			
			// clean up
			// looks deceptively the same as the above check but it's not since this is using Count
			// which is +1 of int i above.
			if ((wordNumBuf.Count % 2) != 0)
			{
				result += prev;
			}
			
			wordNumBuf.Clear();
			ReceiveToken(Token.Number, result.ToString());
		}
		
		int getPrecedence(string lexeme)
		{
			switch (lexeme)
			{
			case "+":
			case "-":
				return 0;
			case "*":
			case "/":
				return 1;
			case "^":
				return 2;
			default:
				throw new Exception("TODO unknown precendence for operator: " + lexeme);
			}
		}
		
		decimal evaluate()
		{
			foreach (object val in postfix)
			{
				if (val is decimal)
				{
					stack.Push(val);
					continue;
				}
				
				decimal a, b;
				
				switch ((string) val)
				{
				case "+":
					stack.Push(((decimal) stack.Pop()) + ((decimal) stack.Pop()));
					break;
				case "-":
					a = (decimal) stack.Pop();
					b = (decimal) stack.Pop();
					stack.Push(b - a);
					break;
				case "*":
					stack.Push(((decimal) stack.Pop()) * ((decimal) stack.Pop()));
					break;
				case "/":
					a = (decimal) stack.Pop();
					b = (decimal) stack.Pop();
					stack.Push(b / a);
					break;
				case "^":
					a = (decimal) stack.Pop();
					b = (decimal) stack.Pop();
					// TODO Math.Pow only accepts doubles, gets complicated ...
					// Needs to iteratively refine answer to decimal point of desired precision.
					// stack.Push(Math.Pow(b, a));
					decimal d = b;
					while ((a -= 1) > 0)
					{
						Console.WriteLine("a:" + a);
						if (a < 1)
						{
							d *= (a*b);
							Console.WriteLine("" + (a*b));
						}
						else
						{
							d *= b;
						}
						Console.WriteLine("d:" + d);
					}
					stack.Push(d);
					break;
				default:
					throw new Exception("TODO Unhandled operator: " + val);
				}
			}
			return (decimal) stack.Pop();
		}
	}
	
	/// <summary>
	/// Run in console.
	/// </summary>
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.Write("input: ");
			string input = Console.ReadLine();
			
			Parser parser = new Parser();
			decimal val = parser.Parse(input);
			//Console.Write("Eval: " + val.ToString("#." + new string('#', 30)));
			Console.Write("Eval: " + val.ToString("G", CultureInfo.InvariantCulture));
		}
	}
	
	/// <summary>
	/// Web service. Uncomment first line of file and rename to aspx.
	/// </summary>
	public class MainService : WebService
	{
		[WebMethod]
		public string Evaluate(string input)
		{
			Parser parser = new Parser();
			decimal val = parser.Parse(input);
			return val.ToString(val.ToString("G", CultureInfo.InvariantCulture));
		}
	}
}
