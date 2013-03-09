using System;
using Mono.Cecil;

namespace AotCompatlyzer
{
	public interface ITypeProcessor : IProcessor
	{
		
		void OnType(TypeDefinition type);
	}
	
	public interface IMethodProcessor : IProcessor
	{
		void OnMethod(MethodDefinition method);
	}
	
	public interface IProcessor
	{
		void OnFile(string fileName, ModuleDefinition module);
		
		void OnDone();
	}
}

