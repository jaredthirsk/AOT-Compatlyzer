using System;
using System.Linq;
using Mono.Cecil;
using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace AotCompatlyzer
{

	#region MSIL Reference

	// Used monodis to get the desired add_/remove_ methods from a simpler C# implementation:
	
	//		// method line 2581
	//		.method private hidebysig specialname 
	//			instance default void add_MyHandlerZYX (class MyNamespace.ActionBool 'value')  cil managed 
	//		{
	//			// Method begins at RVA 0x20000
	//			// Code size 24 (0x18)
	//			.maxstack 8
	//				IL_0000:  ldarg.0 
	//					IL_0001:  ldarg.0 
	//					IL_0002:  ldfld class MyNamespace.ActionBool MyNamespace.EnableableBase::myHandler
	//					IL_0007:  ldarg.1 
	//					IL_0008:  call class [mscorlib]System.Delegate class [mscorlib]System.Delegate::Combine(class [mscorlib]System.Delegate, class [mscorlib]System.Delegate)
	//					IL_000d:  castclass MyNamespace.ActionBool
	//					IL_0012:  stfld class MyNamespace.ActionBool MyNamespace.EnableableBase::myHandler
	//					IL_0017:  ret 
	//		} // end of method EnableableBase::add_MyHandlerZYX
	
	//		// method line 2846
	//		.method private hidebysig specialname 
	//			instance default void remove_MyHandlerZYX (class MyNamespace.ActionBool 'value')  cil managed 
	//		{
	//			// Method begins at RVA 0x226d4
	//			// Code size 24 (0x18)
	//			.maxstack 8
	//				IL_0000:  ldarg.0 
	//					IL_0001:  ldarg.0 
	//					IL_0002:  ldfld class Namespace.ActionBool MyNamespace.EnableableBase::myHandler
	//					IL_0007:  ldarg.1 
	//					IL_0008:  call class [mscorlib]System.Delegate class [mscorlib]System.Delegate::Remove(class [mscorlib]System.Delegate, class [mscorlib]System.Delegate)
	//					IL_000d:  castclass MyNamespace.ActionBool
	//					IL_0012:  stfld class MyNamespace.ActionBool MyNamespace.EnableableBase::myHandler
	//					IL_0017:  ret 
	//		} // end of method EnableableBase::remove_MyHandlerZYX
	
	#endregion
	
	public class ReplaceCE : ITypeProcessor
	{
		public int Verbosity {get{return ProcessorDispatcher.Verbosity;}}

		int replaced = 0;
		int skipped = 0;

		public void OnDone()
		{
			if(Verbosity >= Verbosities.Summary)
				Console.WriteLine(" - Replaced " + replaced + " event methods (" + skipped + " skipped) ");
		}

		public void OnFile(string fileName, ModuleDefinition module)
		{
			replaced = 0;
			skipped = 0;

			TypeReference delegateTypeDef = module.Import(typeof(Delegate));
			
			 combineR = delegateTypeDef.Module.Import(
				typeof(Delegate).GetMethod("Combine", 
			                           new Type[] { typeof(Delegate), typeof(Delegate) })); 
			
			removeR = delegateTypeDef.Module.Import(
				typeof(Delegate).GetMethod("Remove", 
			                           new Type[] { typeof(Delegate), typeof(Delegate) })); 
		}

		MethodReference combineR;
		MethodReference removeR;

		public ReplaceCE()
		{
			Console.WriteLine("Replacing add_* and remove_* methods with versions that do not contain CompareExchange<> (may not be theradsafe)");

//			if(ProcessorDispatcher.Verbosity >= 9)
//				Console.WriteLine(typeof(Delegate).Module.FullyQualifiedName);
		}

		public void OnType(TypeDefinition type)
		{
			if(type.IsInterface)
				return;
			foreach(var method in type.Methods) 
			{
				try 
				{
					string fieldName;
					MethodReference methodR;
					
					#region Get required info or continue
					
					if(method.Name.StartsWith("add_")) {
						fieldName = method.Name.Substring("add_".Length);
						methodR = combineR;
					} else if(method.Name.StartsWith("remove_")) {
						fieldName = method.Name.Substring("remove_".Length);
						methodR = removeR;
					} else {
						continue;
					}
					
#endregion
					
					var newI = new List<Instruction>();
					
					var processor = method.Body.GetILProcessor();
					
					bool foundCompExch = false;
					foreach(var existingInstruction in processor.Body.Instructions) {
						if(existingInstruction.OpCode == OpCodes.Call) {
							var meth = existingInstruction.Operand as MethodReference;
							if(meth != null) {
								if(meth.Name == "CompareExchange") {
									if(Verbosity > 7)
										Console.WriteLine("   Found CompareExchange");
									foundCompExch = true;
									break;
								}
							}
						}
					}
					
					if(!foundCompExch) {
						if(Verbosity >= Verbosities.SkippingVerbose) {
							Console.WriteLine(" . ignoring body with no CompareExchange: " + type.Name + "." + method.Name);
						}
						skipped++;
						continue;
					}
					
					FieldReference field = type.Fields.Where(f => f.Name == fieldName).FirstOrDefault();
					
					if(field == null)
						throw new Exception("Could not find field: " + fieldName + " in type " + type.FullName);
					
					//	IL_0000:  ldarg.0 
					newI.Add(processor.Create(OpCodes.Ldarg_0));
					//	IL_0001:  ldarg.0 
					newI.Add(processor.Create(OpCodes.Ldarg_0));
					
					//	IL_0002:  ldfld class MyNamespace.ActionBool MyNamespace.EnableableBase::myHandler
					newI.Add(processor.Create(OpCodes.Ldfld, field));
					
					//	IL_0007:  ldarg.1 
					newI.Add(processor.Create(OpCodes.Ldarg_1));
					
					//	IL_0008:  call class [mscorlib]System.Delegate class [mscorlib]System.Delegate::Combine(class [mscorlib]System.Delegate, class [mscorlib]System.Delegate)
					newI.Add(processor.Create(OpCodes.Call, methodR));
					
					//	IL_000d:  castclass MyNamespace.ActionBool
					newI.Add(processor.Create(OpCodes.Castclass, field.FieldType));
					
					//	IL_0012:  stfld class MyNamespace.ActionBool MyNamespace.EnableableBase::myHandler
					newI.Add(processor.Create(OpCodes.Stfld, field));
					
					//	IL_0017:  ret 
					newI.Add(processor.Create(OpCodes.Ret));
					
					#region Replace the instructions
					
					replaced++;
					processor.Body.Instructions.Clear();
					foreach(var i in newI) {
						processor.Body.Instructions.Add(i);
					}
					
					if(Verbosity >= Verbosities.Success) {
						Console.WriteLine(" - replaced method: " + type.Name + "." + method.Name);
					}
					
#endregion
					
				} catch(Exception ex) {
					Console.WriteLine("Exception for method: " + type.FullName + "." + method.Name + ": " + ex);
				}
			}
		}
	}
}

