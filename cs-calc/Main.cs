using System;
using System.Collections;

namespace cscalc
{
	public enum Token
	{
		Number,
		Operator,
		OpenParanthesis,
		CloseParanthesis
	}
	
	/**
	 * Defines current state of lexer for processing next input
	 */
	delegate State State(Lexer lexer);
	
	/**
	 * Interface to implement for handling emitted tokens of Lexer instance
	 */
	interface ITokenReceiver
	{
		void ReceiveToken(Token token, string lexeme);
	}
	
	/**
	 * Simple receiver that prints token to console
	 */
	class PrinterReceiver : ITokenReceiver
	{
		public void ReceiveToken(Token token, string lexeme)
		{
			Console.WriteLine("received token: {0}, {1}", token, lexeme);
		}
	}
	
	/**
	 * Tokenizes input
	 */
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
		
		public static State lexWhitespace(Lexer l)
		{
			switch (l.read())
			{
			case ' ':
				l.discard();
				return lexWhitespace;
			case '(':
				l.emit(Token.OpenParanthesis);
				l.discard();
				return lexWhitespace;
			case ')':
				l.emit(Token.CloseParanthesis);
				l.discard();
				return lexWhitespace;
			case '0':
			case '1':
			case '2':
			case '3':
			case '4':
			case '5':
			case '6':
			case '7':
			case '8':
			case '9':
				l.reset();
				return lexNumber;
			case '+':
			case '-':
			case '*':
			case '/':
				l.reset();
				return lexOperator;
			case EOF:
				return null;
			default:
				throw new Exception("Invalid input.");
			}
		}
		
		public static State lexNumber(Lexer l)
		{
			switch (l.read())
			{
			case '0':
			case '1':
			case '2':
			case '3':
			case '4':
			case '5':
			case '6':
			case '7':
			case '8':
			case '9':
				l.next();
				return lexNumber;
			case EOF:
				l.emit (Token.Number);
				return null;
			default:
				l.emit(Token.Number);
				l.reset();
				return lexWhitespace;
			}
		}
		
		public static State lexOperator(Lexer l)
		{
			switch (l.read())
			{
			case '+':
			case '-':
			case '*':
			case '/':
				l.next();
				return lexOperator;
			case EOF:
				l.emit (Token.Operator);
				return null;
			default:
				l.emit(Token.Operator);
				l.reset();
				return lexWhitespace;
			}
		}
	}
	
	/**
	 * 
	 */
	class Parser : ITokenReceiver
	{
		ArrayList postfix;
		Stack stack;
		
		public Parser()
		{
			postfix = new ArrayList();
			stack = new Stack();
		}
		
		public decimal Parse(string input)
		{
			//ITokenReceiver receiver = new PrinterReceiver();
			//Lexer lexer = new Lexer(receiver, input);
			Lexer lexer = new Lexer(this, input);
			lexer.Run();
			while (stack.Count != 0)
			{
				postfix.Add(stack.Pop());
			}
			return evaluate();
		}
		
		public void ReceiveToken(Token token, string lexeme)
		{
			switch (token)
			{
			case Token.Number:
				postfix.Add(decimal.Parse(lexeme));
				break;
			case Token.OpenParanthesis:
				// TODO
				break;
			case Token.CloseParanthesis:
				// TODO
				break;
			case Token.Operator:
				if (stack.Count == 0)
				{
					stack.Push(lexeme);
				}
				else
				{
					string s = stack.Peek() as string;
					if ((s.Equals("*") || s.Equals("/")) && (lexeme.Equals("+") || lexeme.Equals("-")))
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
				
				switch (val as string)
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
				default:
					throw new Exception("TODO Unhandled operator: " + val);
				}
			}
			return (decimal) stack.Pop();
		}
	}
	
	/**
	 * 
	 */
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.Write("input: ");
			string input = Console.ReadLine();
			
			Parser parser = new Parser();
			decimal val = parser.Parse(input);
			if (val % 1 == 0)
			{
				Console.Write ("Eval: " + ((double) val));
			}
			else
			{
				Console.Write ("Eval: " + val);
			}
		}
	}
}
