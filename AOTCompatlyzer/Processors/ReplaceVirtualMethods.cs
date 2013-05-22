//#define CUSTOM // For a custom blacklist
using System;
using System.Linq;
using Mono.Cecil.Cil;
using Mono.Cecil;
using Mono.Collections.Generic;
using System.Collections.Generic;
using System.Reflection;

namespace AotCompatlyzer
{
	public static class Verbosities
	{
		public const int Error = 1;
		public const int Summary = 1;
		public const int Warning = 2;
		public const int Skipping = 6;
		public const int SkippingVerbose = 7;
		public const int Success = 5;
		public const int Debug = 8;
		public const int ExtraTrace = 9;
	}

	public class ReplaceVirtualMethods : IMethodProcessor
	{
		int replaced = 0;
		int skipped = 0;

		int Verbosity { get { return AotCompatlyzer.Verbosity; } }

		public ReplaceVirtualMethods ()
		{
		}

		List<AssemblyDefinition> ads = new List<AssemblyDefinition> ();

		public void OnFile (string fileName, ModuleDefinition module)
		{
			replaced = 0;
			skipped = 0;


			ads.Add (module.Assembly);
			foreach (var ar in module.AssemblyReferences) {
				try {
					AssemblyDefinition a = module.AssemblyResolver.Resolve (ar);
					if (Verbosity >= 7)
						Console.WriteLine ("Loaded assembly: " + a.Name);
					ads.Add (a);
				} catch {
					Console.WriteLine ("Failed to resolve assembly: " + ar.Name);
				}
			}

		}
		#region IMethodProcessor implementation

		public void OnMethod (MethodDefinition method)
		{

			if (method.DeclaringType.IsInterface)
				return;
			if (!method.HasBody) {
				if (Verbosity >= 9)
					Console.WriteLine ("method has no body: " + method.FullName);
				return;
			}

#if true // Can some good still be done in generic methods?
			if (method.GenericParameters.Count > 0) {
				if (Verbosity >= Verbosities.SkippingVerbose)
					Console.WriteLine (" . Skipping generic method: " + method.FullName);
				return;
			}
#endif

			var processor = method.Body.GetILProcessor ();

			foreach (var i in method.Body.Instructions.ToArray()) {
				if (i.OpCode != OpCodes.Callvirt
					&& i.OpCode != OpCodes.Call
					&& i.OpCode != OpCodes.Calli
				   )
					continue;

				MethodReference mr = i.Operand as MethodReference;

				if (mr == null)
					throw new Exception ("Call method does not have MethodReference as Operand.");

				if (!mr.IsGenericInstance) {
					continue;
				} // Skipped

				switch (OnInvocation (method, processor, i, mr)) {
				case InvocationResult.Skipped:
					if(Verbosity <= Verbosities.Skipping)
					{
						Console.WriteLine ("<<skipped>> "
							+ mr.FullName + " in " + method.DeclaringType.FullName + "." + method.Name + "]]");
					}
					break;
				case InvocationResult.Succeeded:
					//Console.WriteLine("<<succeeded>> "
					  //                + mr.FullName + " in " + method.DeclaringType.FullName + "."+ method.Name + "]]");
					break;
				case InvocationResult.Failed:

					//if(mr.FullName.Contains ("")) break;
//					if(mr.FullName.Contains ("")) break;
//					if(mr.FullName.Contains ("")) break;
					if(mr.FullName.Contains ("LionFire.Collections.MultiBindableEvents::RaiseCollectionChanged<T>(LionFire.Collections.NotifyCollectionChangedHandler`1<!!0>,System.Collections.Specialized.NotifyCollectionChangedEventArgs)")) break;
					if(mr.FullName.Contains ("LionFire.DictionaryExtensions::TryGetValue<System.String,LionFire.ObjectBus.IHandle>(System.Collections.Generic.IDictionary`2<!!0,!!1>,!!0)")) break;
					if(mr.FullName.Contains ("LionFire.ObjectBus.VobHandle`1<!!0> LionFire.ObjectBus.VosContextExtensions::ToAssetVobHandle<LionFire.Assets.Package>(System.String)")) break;
					if(mr.FullName.Contains ("System.Linq.Enumerable::Count<System.Char>(System.Collections.Generic.IEnumerable`1<!!0>,System.Func`2<!!0,System.Boolean>)")) break;
					if(mr.FullName.Contains ("System.Linq.Enumerable::SingleOrDefault<LionFire.ObjectBus.Mount>(System.Collections.Generic.IEnumerable`1<!!0>)")) break;
					if(mr.FullName.Contains ("System.Collections.Generic.IEnumerable`1<!!0> System.Linq.Enumerable::OfType<T>(System.Collections.IEnumerable)")) break;
					if(mr.FullName.Contains ("System.Linq.Enumerable::ToArray<T>(System.Collections.Generic.IEnumerable`1<!!0>")) break;
					if(mr.FullName.Contains ("System.Boolean System.Linq.Enumerable::Any<System.String>(System.Collections.Generic.IEnumerable`1<!!0>)")) break;
					if(mr.FullName.Contains ("System.Collections.Generic.IEnumerable`1<!!0> System.Linq.Enumerable::OfType<T>(System.Collections.IEnumerable)")) break;
					if(mr.FullName.Contains ("System.Collections.Generic.IEnumerable`1<!!0> System.Linq.Enumerable::Where<System.Type>(System.Collections.Generic.IEnumerable`1<!!0>,System.Func`2<!!0,System.Boolean>)")) break;

					//					if(mr.FullName.Contains ("")) break;

					// Broad
					if(mr.FullName.Contains ("System.Linq")) break;


					Console.WriteLine ("<<failed>> "
						+ mr.FullName + " in " + method.DeclaringType.FullName + "." + method.Name + "]]");


					if(AotCompatlyzer.TraceMode)
					{
						MethodInfo writeLineMethod = 
							typeof(Console).GetMethod("WriteLine", new Type[]{typeof(string)});

						MethodReference writeLine;
						writeLine = method.Module.Assembly.MainModule.Import(writeLineMethod);

						string sentence = "[[Invoking " + mr.FullName + " in " + method.DeclaringType.FullName + "." + method.Name + "]]";
						sentence += "-" + sentence.GetHashCode();
						Instruction insertSentence;
						insertSentence = processor.Create(OpCodes.Ldstr, sentence);
						Instruction callWriteLine;
						callWriteLine = processor.Create(OpCodes.Call, writeLine);

						Instruction ins = i;
							//method.Body.Instructions[0];

						processor.InsertBefore(ins, insertSentence);
						processor.InsertAfter(insertSentence, callWriteLine);
					}
					if(AotCompatlyzer.TraceMode)
					{
						MethodInfo writeLineMethod = 
							typeof(Console).GetMethod("WriteLine", new Type[]{typeof(string)});
						
						MethodReference writeLine;
						writeLine = method.Module.Assembly.MainModule.Import(writeLineMethod);
						
						string sentence = "[[Invoked " + mr.FullName + " in " + method.DeclaringType.FullName + "." + method.Name + "]]";
						sentence += "-" + sentence.GetHashCode();

						Instruction insertSentence;
						insertSentence = processor.Create(OpCodes.Ldstr, sentence);
						Instruction callWriteLine;
						callWriteLine = processor.Create(OpCodes.Call, writeLine);
						
						Instruction ins = i;
						//method.Body.Instructions[0];
						
						processor.InsertAfter(ins, insertSentence);
						processor.InsertAfter(insertSentence, callWriteLine);

					}
					break;
				default:
					throw new Exception ("Unknown InvocationResult");
				}
			}
		}
		public enum InvocationResult
		{
			Succeeded,
			Failed,
			Skipped
		}

		private InvocationResult OnInvocation (MethodDefinition method, ILProcessor processor, Instruction i, MethodReference mr)
		{
			Mono.Cecil.GenericParameter genPar = null;

			if (!mr.FullName.Contains ("<")) {
				//if(Verbosity >= 8) - shouldn't be reached
				Console.WriteLine ("[UNREACHABLE] Skipping method that contains no <");
				return InvocationResult.Skipped;
			}
					
			if (mr.FullName.Contains (" System.")) {
				// REVIEW - does AOT somehow support some of these cases?
				if (Verbosity >= Verbosities.Skipping)
					Console.WriteLine ("Skipping method invocation that contains ' System.': [["
						+ mr.FullName + " in " + method.DeclaringType.FullName + "." + method.Name + "]]");
				return InvocationResult.Failed;
			}
					
			#if CUSTOM // Custom blacklist
					if(!mr.FullName.Contains(" LionFire.") && !mr.FullName.Contains(" Dycen."))
					{
						if(Verbosity >= Verbosities.Skipping)Console.WriteLine("Skipping method that is not LionFire or Dycen:" 
						                                                       + mr.FullName + " in " + method.DeclaringType.FullName + "."+ method.Name);
						//continue;
						return InvocationResult.Skipped
					}
			#endif
					
			var genPars = mr.Resolve ().GenericParameters;
			//					Console.WriteLine("TEMP2 - " + genPars.Count);
			//				var genPars = mr.GetGenericParameters(method.Module);
			//				Console.WriteLine("TEMP " + mr.Name);
			//				Console.WriteLine("TEMP genPars.Count " + genPars.Count);
					
			if (genPars.Count != 1) {
				if (Verbosity >= Verbosities.Warning)
					Console.WriteLine ("[NS] Replacing methods with more than 1 generic parameter not supported: " + genPars.Count + ": " + mr.FullName + " in " + method.DeclaringType.FullName + "." + method.Name);
				return InvocationResult.Failed;
			} else {
				genPar = genPars [0];
				//					var resolved = genPar.Resolve();
				//					Console.WriteLine("NEW -- <" + (resolved == null ? "null" : resolved.Name) + ">");
				if (Verbosity >= 10)
					Console.WriteLine ("NEW |- <" + genPar + ">");
			}
					
					
			#region string genericParameter = ...;
			string genericParameter;
			Type genericTypeParameter;
			TypeDefinition genericTypeParameterDefinition = null;
			{
				string n = mr.FullName.Split (' ') [1];
				n = n.Split (new string[]{"::"}, StringSplitOptions.RemoveEmptyEntries) [1];
				int startI = n.IndexOf ('<') + 1;
				int stack = 0;
				int endI = startI + 1;
				while (stack > 0 || n[endI] != '>') {
					if (n [endI] == '<')
						stack++;
					if (n [endI] == '>')
						stack--;
					endI++;
				}
						
				int length = endI - startI;
				genericParameter = n.Substring (startI, length);
						
				//					if(genericParameter.StartsWith("!!"))
				//					{
				//						int genParAliasIndex = Convert.ToInt32(genericParameter.Substring(2));
				//
				//						var genParAlias = genPars[genParAliasIndex];
				//
				//
				//						genericParameter = genParAlias.FullName;
				//						Console.WriteLine("NEW - Generic method alias param: " + genericParameter);
				//					}
				//				if(genericParameter.Contains("<") || genericParameter.Contains(">"))
				//				{
				//					Console.WriteLine("Unsupported generic method ("+mr.FullName+") with generic parameter: " + genericParameter);
				//					skipped++;
				//					continue;
				//				}
						
				if (Verbosity >= 8)
					Console.WriteLine ("Generic method param: " + genericParameter);
						
				genericTypeParameter = Type.GetType (genericParameter, false);
						
				//if(genericTypeParameter == null) 
				{
					foreach (ModuleDefinition modDef in ads.SelectMany(assDef => assDef.Modules)) {
						//						foreach(var modType in modDef.Types)
						//						{
						//							Console.WriteLine("ccc - " + modType);
						//						}
						genericTypeParameterDefinition = modDef.Types.Where (td => td.FullName == genericParameter
								                                                    //							                      && !td.IsGenericInstance
						).FirstOrDefault ();
								
						if (genericTypeParameterDefinition != null) {
							if (Verbosity >= 9)
								Console.WriteLine ("TODO - got genTD: " + genericTypeParameterDefinition);
							break;
						}
					}
					if (genericTypeParameterDefinition == null) {
						if (Verbosity >= 8)
							Console.WriteLine (" x Could not get TypeDefinition for " + genericParameter);
						// No continue, this is not a problem
					}
							
					if (genericTypeParameter == null && genericTypeParameterDefinition == null) {
						if (Verbosity >= Verbosities.Error) {
							Console.WriteLine (" x - Failed to get Type for " + genericParameter + " in invocation: " + mr.FullName + " in method " + method.FullName); 
						}
						skipped++;
						return InvocationResult.Failed;
					}
				}
			}
			#endregion
					
			#if ONLY_VOID_OR_GENPARM // OLD, now other return types are supported if they are the same as replaced method
					string matchingReturnType = "!!0";
					//				genericTypeParameter.FullName;
					
					
					if(mr.ReturnType.FullName != "System.Void" 
					   && mr.ReturnType.FullName != matchingReturnType)
						//				   && mr.ReturnType.FullName != genericTypeParameter.FullName) 
					{
						if(Verbosity >= 3)
							Console.WriteLine("   x generic method doesn't return System.Void or '"
							                  +matchingReturnType+
							                  "': " + mr.FullName+ ", but instead: " + mr.ReturnType.FullName);
						continue;
					}
			#endif
					
			//				if(Verbosity >= 9) Console.WriteLine("mr: " + mr.Name);
					
			TypeDefinition tr = mr.DeclaringType.Resolve ();
					
			MethodDefinition replacementMethod = null;
			//				if(mr.DeclaringType.Name == "MultiType")
			//				{
			//					foreach(MethodDefinition replacementMethod_ in 
			//						tr.Methods)
			//					{
			//						Console.WriteLine("z " + replacementMethod_.Name + "    " + replacementMethod_.FullName);
			//					}
			//				}
					
			bool noCast = false;
					
			foreach (MethodDefinition replacementMethod_ in 
					        tr.Methods.Where(mr_ => mr_.Name == mr.Name && !mr_.HasGenericParameters)) {
				noCast = false;
				// TODO: Verify parameters
				if (!replacementMethod_.HasParameters)
					continue;
				if (replacementMethod_.Parameters.Count != mr.Parameters.Count + 1) {
					if (Verbosity >= 8)
						Console.WriteLine (" [x? param count] - (alt) candidate replacement method has wrong parameter count: " + replacementMethod_.FullName
							+ " [[" + mr.FullName + " in " + method.DeclaringType.FullName + "." + method.Name + "]]");
					continue;
				}
				//					Console.WriteLine("Replacement param type: "+ replacementMethod_.Parameters[0].ParameterType.FullName);
						
				if (replacementMethod_.Parameters [replacementMethod_.Parameters.Count - 1].ParameterType.FullName != "System.Type") {
					if (Verbosity >= 8)
						Console.WriteLine (" [x? param type] - (alt) candidate replacement does not have Type parameter at the end of parameters : " + replacementMethod_.FullName
							+ " [[" + mr.FullName + " in " + method.DeclaringType.FullName + "." + method.Name + "]]");
					continue;
				}
						
						
				if (mr.ReturnType.FullName == replacementMethod_.ReturnType.Resolve ().FullName) {
					noCast = true;
					if (Verbosity >= 9)
						Console.WriteLine ("   - (alt) generic method and alt method return same type:: " + mr.ReturnType.FullName);
				} else if (mr.ReturnType.FullName != "System.Void") { // Replacement must return object
					if (replacementMethod_.ReturnType.Resolve ().FullName != "System.Object") {
						if (Verbosity >= 3)
							Console.WriteLine (" [x? return] (alt) generic method returns T but candidate replacement method does not return System.Object: " + replacementMethod_.FullName
								+ " [[" + mr.FullName + " in " + method.DeclaringType.FullName + "." + method.Name + "]]");
						continue;
					}
				}
				if (Verbosity >= 8)
					Console.WriteLine ("FOUND ALTERNATE METHOD: " + replacementMethod_);
				replacementMethod = replacementMethod_;
				break; // FUTURE: don't break here, keep going to see if there are multiple (ambiguous) matches and throw/output error
			}
			if (replacementMethod == null) {
				if (!(" " + mr.FullName).Contains (" System.")) {
					if (Verbosity >= Verbosities.Warning)
						Console.WriteLine ("[__] No alternate found for [[" 
							+ mr.FullName + " in " + method.DeclaringType.FullName + "." + method.Name + "]]");
					//						                  + mr.DeclaringType.FullName+"." +mr.Name + "(...)");
				}
				skipped++;
				return InvocationResult.Failed;
			}
					
			//				if(mr.Name != "TestMethod") continue; // TEMP
			if (Verbosity >= Verbosities.Success)
				Console.WriteLine (" O Replacing " + mr.FullName 
					+ " " + mr.GenericParameters.Count + " generic parameters"
					+ " " + mr.Parameters.Count + " parameters"
					+ " | " + mr.GetElementMethod ().FullName + ""
					+ " | " + mr.GetElementMethod ().HasGenericParameters + ""
					+ " | " + mr.GetElementMethod ().GenericParameters [0].Name + ""
				);
					
			//				if(Verbosity >= 6)
			//					Console.WriteLine("Resolved non-specific generic method: " + mr.FullName);
					
			//				if(Verbosity >= 8) Console.WriteLine("RESOLVED TYPE: " + genericTypeParameter);
					
			//				var typeModuleDefinition = ModuleDefinition.ReadModule(type.Module.Assembly.Location);
			//				var typeDefinition = typeModuleDefinition.Types.Where(td => td.FullName == genericParameter).FirstOrDefault();
			//				if(typeDefinition != null && Verbosity >= 5) 
			//				{
			//					Console.WriteLine("Resolved typeDefinition: " + typeDefinition);
			//				}
			//				else
			//				{
			//					Console.WriteLine("Failed to resolve typeDefinition: " + type.FullName);
			////					foreach(var td in ModuleDefinition.ReadModule(type.Module.Assembly.Location).Types)
			////					{
			////						Console.WriteLine(" ... " + td.FullName);
			////					}
			//					continue;
			//				}
					
			//				method.Module.Import(type); // try removing this
					
			//				IMetadataScope scope = method.Module;
			//				var typeRef = new TypeReference(type.Namespace, type.Name, typeModuleDefinition, scope, type.IsValueType);
			//				Console.WriteLine("TypeRef: "+ typeRef);
					
			//				method.Module.Import(type);
			var replacementMethodImported = method.Module.Import (replacementMethod);
					
			// IL_0000:  ldtoken Rewriter.TestClass
					
			if (genericTypeParameter != null) {
				processor.InsertBefore (i, processor.Create (OpCodes.Ldtoken, method.Module.Import (genericTypeParameter)));
			} else {
				processor.InsertBefore (i, processor.Create (OpCodes.Ldtoken, method.Module.Import (genericTypeParameterDefinition)));
			}
					
			// IL_0005:  call class [mscorlib]System.Type class
			//              [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
					
			var gtfh = typeof(Type).GetMethod ("GetTypeFromHandle");
			MethodReference gtfhRef = method.Module.Import (gtfh, mr);
					
			processor.InsertBefore (i, processor.Create (OpCodes.Call, gtfhRef));
					
			// IL_000a:  call void class Rewriter.TestClass::TestMethod(class [mscorlib]System.Type)
			var callMethod = processor.Create (i.OpCode, replacementMethodImported);
			processor.InsertAfter (i, callMethod);
					
			#region Cast the result, if it exists
					
			if (mr.ReturnType.FullName != "System.Void" && !noCast) {
				string castAssembly;
				string castType;
						
				if (genericTypeParameter != null) {
					castAssembly = genericTypeParameter.Assembly.GetName (false).Name;
					castType = "[" + castAssembly + "]" + genericTypeParameter.FullName;
				} else if (genericTypeParameterDefinition != null) {
					castAssembly = "";
					castType = genericTypeParameterDefinition.ToString ();
					//						var resolvedGTPD = genericTypeParameterDefinition.Resolve();
					//						resolvedGTPD.FullName
				} else {
					castType = "???";
					Console.WriteLine ("INTERNAL ERROR - genericTypeParameter not set for " + mr.FullName + ". genericTypeParameterDefinition:" + genericTypeParameterDefinition.Resolve ());
					return InvocationResult.Failed;
				}
						
				//					castAssembly = castAssembly.Substring(castAssembly.IndexOf(","));
				if (Verbosity > 8)
					Console.WriteLine ("CAST to " + castType + " | " + genericTypeParameterDefinition);
				var importedGenericType = mr.Module.Import (genericTypeParameterDefinition);
				processor.InsertAfter (callMethod,
						                      processor.Create (OpCodes.Castclass, importedGenericType));
			}
					
			#endregion
					
			processor.Remove (i);
			replaced++;
					
			//				if(Verbosity >= Verbosities.Success)
			//					Console.WriteLine(" - " + ((MethodReference)i.Operand).Name + " replaced with " + replacementMethod.FullName);
					
					
			//				mr.GetGenericParameters(null);
			//
			//			if(method.GenericParameters.Count == 0) return;
			//
			//			if(method.GenericParameters.Count > 1)
			//			{
			//				Console.WriteLine("Warning: cannot handle more than one generic parameter yet: " + 
			//				                  method.DeclaringType.FullName + "." + method.Name);
			//				return;
			//			}
			//
			//			var body = method.Body;
			//			body.Instructions
			//			if(method.GenericParameters.Count == 1)
			//			{
			//			}

			return InvocationResult.Succeeded;
		}

		#endregion

		#region IProcessor implementation

		public void OnDone ()
		{
			if (Verbosity >= Verbosities.Summary)
				Console.WriteLine (" - Replaced " + replaced + " generic methods " + (skipped <= 0 ? "" : "(" + skipped + " skipped) "));

		}

		#endregion
	}
}

