//#define BUILTINTESTS
//#define CUSTOM

using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace AotCompatlyzer
{
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
				fileList.AddRange(args);
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
