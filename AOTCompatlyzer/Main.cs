//#define BUILTINTESTS
//#define CUSTOM

using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace AotCompatlyzer
{
	public static class AotCompatlyzer
	{
		public static int Verbosity = (int)Verbosities.Warning;

		public static bool TraceMode = false;
		public static bool PretendMode = true;
	}

	class MainClass
	{

		public static void Main(string[] args)
		{
			Console.WriteLine(" ================================ ");
			
			ProcessorDispatcher d = new ProcessorDispatcher();

#if  BUILTINTESTS
			var x = new TestClass();
			x.DoTestClassTest_string();
//			x.DoTestClassTest<string>();
#endif

			List<string> fileList = new List<string>();


			if(args.Length > 0)
			{
				IEnumerable<string> fileListArgs = args;
				if(args[0].StartsWith ("-"))
				{
					int verb;
					if(Int32.TryParse (args[0].Substring(1), out verb))
					{
						AotCompatlyzer.Verbosity = verb;
						fileListArgs = fileListArgs.Skip(1);
					}
				}

				if (fileListArgs.Contains ("--trace")) {
					AotCompatlyzer.TraceMode = true;
					fileListArgs = fileListArgs.Where(a => a !="--trace");
					Console.WriteLine (" --- TRACE MODE --- ");
				}
				if (fileListArgs.Contains ("--pretend")) {
					AotCompatlyzer.PretendMode = true;
					fileListArgs = fileListArgs.Where(a => a !="--pretend");
					Console.WriteLine (" --- PRETEND MODE --- ");
				}

				fileList.AddRange(fileListArgs);
			}
			else
			{
				foreach(var fileName in
				        Directory.GetFiles(Directory.GetCurrentDirectory(), "*.dll")
				        .Concat(Directory.GetFiles(Directory.GetCurrentDirectory(), "*.exe"))) 
				{
#if false // Rewriter only
					if(!fileName.Contains("AOTCompatlyzer"))continue;
#else
#endif
					fileList.Add(fileName);
				}
			}

			Console.WriteLine("Verbosity: " + AotCompatlyzer.Verbosity);


			foreach(var fileName in fileList){

				// Blacklist could go here
#if CUSTOM
				if(fileName.EndsWith("LionRing.dll")) continue;
#endif

				try {
					string outFileName = fileName + "-orig-";
					while (File.Exists(outFileName)) {
						outFileName += "-";
					}
					d.OnFile(fileName, outFileName);
				} catch(Exception ex) {
					Console.WriteLine("Exception processing dll " + fileName + ": " + ex);
				}
			}
		}

	}
}
