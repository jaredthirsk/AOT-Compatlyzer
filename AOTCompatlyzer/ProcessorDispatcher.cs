using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

namespace AotCompatlyzer
{
	
	public class ProcessorDispatcher
	{
		public static int Verbosity {get{return AotCompatlyzer.Verbosity;}}
		
		public ProcessorDispatcher()
		{
			TypeProcessors.Add(new ReplaceCE());
			MethodProcessors.Add(new ReplaceVirtualMethods());
		}
		
		List<ITypeProcessor> TypeProcessors = new List<ITypeProcessor>();
		List<IMethodProcessor> MethodProcessors = new List<IMethodProcessor>();
		
		public IEnumerable<IProcessor> Processors {
			get {
				foreach(var tp in TypeProcessors)
					yield return tp;
				foreach(var mp in MethodProcessors)
					yield return mp;
			}
		}
		
		public void OnFile(string fileName, string outFileName, bool keepOriginal = false, bool swap = true)
		{
			Console.WriteLine(" ----- " + fileName + " -----");
			if(ProcessorDispatcher.Verbosity >= 4)
				Console.WriteLine("" + fileName + " --> " + outFileName);
			
			if(!File.Exists(fileName))
				throw new ArgumentException("File not found: " + fileName);
			
			AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(fileName);
			ModuleDefinition module = assembly.MainModule;
			
			foreach(var processor in Processors) {
				processor.OnFile(fileName, module);
			}
			
			foreach(TypeDefinition type in module.Types) {
				foreach(var tp in TypeProcessors) {
					tp.OnType(type);
				}
				
				foreach(var method in type.Methods) {
					foreach(var mp in MethodProcessors) {
						mp.OnMethod(method);
					}
				}
			}
			
			if(swap) {
				File.Move(fileName, outFileName);
				assembly.Write(fileName);
				if(!keepOriginal)
					File.Delete(outFileName);
			} else {
				assembly.Write(outFileName);
				if(!keepOriginal)
					File.Delete(fileName);
			}
			
			foreach(var p in Processors) {
				p.OnDone();
			}
			
		}
	}
}

