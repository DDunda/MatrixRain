using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace MatrixRain
{
	class Program
	{
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool SetConsoleMode(IntPtr hConsoleHandle, int mode);
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool GetConsoleMode(IntPtr handle, out int mode);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr GetStdHandle(int handle);

		static void Main(string[] args)
		{
			var handle = GetStdHandle(-11);
			int mode;
			GetConsoleMode(handle, out mode);
			SetConsoleMode(handle, mode | 0x4);

			uint width = (uint)Console.WindowWidth;
			uint height = (uint)Console.WindowHeight;

			ushort minLength = 5;
			ushort maxLength = 10;
			ushort maxRate = 5;

			LongRng rng = new LongRng(349948357);
			Droplet[,] dataMap = new Droplet[width, height];
			Console.BackgroundColor = ConsoleColor.Black;
			ColourScheme colours = new Fire();



			for (uint y = 0; y < height; y++)
				for (uint x = 0; x < width; x++)
				{
					Droplet point = new Droplet();
					point.symbol = '.';
					point.magnitude = 0;
					point.length = 0;
					//point.colour = ConsoleColor.Black;
					dataMap[x, y] = point;
				}

			while (true)
			{
				Console.Clear();

				// If a point has a droplet above it then it becomes equal to that
				// droplet's magnitude, otherwise, the point's value fades to nothing
				for (uint y = height - 1; y >= 0 && y < height; y--)
					for (uint x = 0; x < width; x++)
					{
						Droplet thisDrop = dataMap[x, y];
						if (y != 0)
						{
							Droplet topDrop = dataMap[x, y - 1];
							thisDrop.magnitude = (ushort)(topDrop.magnitude);
							thisDrop.length = topDrop.length;
						}
						else
						{ // Fades to nothing
							if (thisDrop.magnitude != 0)
							{
								thisDrop.magnitude--;
							}
							else
							{
								thisDrop.length = 0;
							}
						}
					}

				// Starts a new droplet at a random x in the top row
				ushort newDrops = (ushort)rng.NextInt((int)maxRate);
				for (int i = 0; i < newDrops; i++)
				{
					Droplet newDrop = dataMap[rng.NextInt((int)width), 0];
					newDrop.length = (ushort)(newDrop.magnitude + rng.NextInt((int)(maxLength - minLength)) + minLength);
					newDrop.magnitude = newDrop.length;
				}

				string strOut = "";

				for (uint y = 0; y < height; y++)
				{
					for (uint x = 0; x < width; x++)
					{
						Droplet thisDrop = dataMap[x, y];
						//Console.ForegroundColor = thisDrop.scheme.GetColour(thisDrop);
						strOut += colours.Render(thisDrop, rng);
					}
					strOut += '\n';
				}
				Console.Write(strOut);
				Thread.Sleep(200);
			}
		}
		abstract class ColourScheme
		{
			protected string lastColour = "";
			public abstract string Render(Droplet point, LongRng rng);
		}
		class Matrix : ColourScheme
		{
			public override string Render(Droplet point, LongRng rng)
			{
				// Sets random character for each point
				point.symbol = (char)(rng.NextInt(93) + 33);

				string strOut = "";
				if (point.magnitude == 0)
					strOut = ("\x1b[38;2;0;0;0m"); // return ConsoleColor.black;
				else if (point.magnitude < point.length - 3)
					strOut = ("\x1b[38;2;0;255;0m"); // return ConsoleColor.green;
				else if (point.magnitude < point.length)
					strOut = ("\x1b[38;2;127;255;127m"); // return ConsoleColor.grey;
				else strOut = ("\x1b[38;2;255;255;255m"); // return ConsoleColor.white;

				if (strOut != lastColour)
				{
					lastColour = strOut;
				}
				else strOut = "";

				return strOut + point.symbol;
			}
		}
		class Fire : ColourScheme
		{
			public override string Render(Droplet point, LongRng rng)
			{
				string strOut = "";
				double percent = (double)point.magnitude / (double)point.length;
				if (point.length == 0) strOut = ("\x1b[38;2;0;0;0m");
				else
				{
					double red = 2 * percent - percent * percent;
					double green = 3 * percent * percent - 2 * percent * percent * percent;
					double blue = percent * percent < 0.25 ? 0 : (4 * percent * percent - 1) / 3.0;
					strOut = "\x1b[38;2;" + ((int)(red * 255)).ToString() + ";" + ((int)(green * 255)).ToString() + ";" + ((int)(blue * 255)).ToString() + "m";
				}
				/*int colour = (blue > 0.25 ? 0x00000001 : 0x00000000)
				| (green > 0.25 ? 0x00000002 : 0x00000000)
				| (red > 0.25 ? 0x00000004 : 0x00000000)
				| ((red + green + blue) > 0.5 ? 0x00000008 : 0);
				return (ConsoleColor)colour;*/
				if (strOut != lastColour)
				{
					lastColour = strOut;
				}
				else strOut = "";

				if (point.magnitude >= point.length)
				{
					point.symbol = "&$%@#"[rng.NextInt(5)];
				}
				else if (percent > 0.75) point.symbol = 'A';
				else point.symbol = '^';

				return strOut + point.symbol;
			}
		}

		class Droplet
		{
			public char symbol;
			//public ConsoleColor colour { get => scheme.GetColour(this); }
			//public ConsoleColor colour;
			public ushort magnitude;
			public ushort length;
		}

		// Credit: https://stackoverflow.com/questions/15463033/c-sharp-randomlong
		public sealed class LongRng
		{
			public LongRng(long seed)
			{
				_seed = (seed ^ LARGE_PRIME) & ((1L << 48) - 1);
			}

			public int NextInt(int n)
			{
				if (n <= 0)
					throw new ArgumentOutOfRangeException("n", n, "n must be positive");

				if ((n & -n) == n)  // i.e., n is a power of 2
					return (int)((n * (long)next(31)) >> 31);

				int bits, val;

				do
				{
					bits = next(31);
					val = bits % n;
				} while (bits - val + (n - 1) < 0);
				return val;
			}

			private int next(int bits)
			{
				_seed = (_seed * LARGE_PRIME + SMALL_PRIME) & ((1L << 48) - 1);
				return (int)(((uint)_seed) >> (48 - bits));
			}

			private long _seed;
			//public long seed { get => _seed; }

			private const long LARGE_PRIME = 0x5DEECE66DL;
			private const long SMALL_PRIME = 0xBL;
		}
	}
}
