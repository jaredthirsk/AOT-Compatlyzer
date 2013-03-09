using System;

namespace AotCompatlyzer
{
	public class TestClass
	{
		public static T2 Test2Method<T,T2>()
		{
			Console.WriteLine("G Type: " + typeof(T2).Name);
			if(typeof(T2) == typeof(string))
			{
				return (T2)(object)"G_string-result";
			}
			return default(T2);
		}

		public static T TestMethod<T>()
		{
			Console.WriteLine("TestMethodG");
			Console.WriteLine("G Type: " + typeof(T).Name);
			if(typeof(T) == typeof(string))
			{
				return (T)(object)"G_string-result";
			}
			return default(T);
		}
		public static object TestMethod(Type type)
		{
			Console.WriteLine("TestMethodNG");
			Console.WriteLine("NG Type: " + type.Name);
			if(type == typeof(string))
			{
				return "NG_string-result";
			}
			return null;
		}
		public static void TestVoid<T>()
		{
			Console.WriteLine("TestMethodVoidG");
			Console.WriteLine("G Void Type: " + typeof(T).Name);
		}
		public static void TestVoid(Type type)
		{
			Console.WriteLine("TestMethodVoidNG");
			Console.WriteLine("Void NG Type: " + type.Name);
		}

		public static string String_Test<T>()
		{
			return "G String_Test " + typeof(T).Name;
		}
		public static string String_Test(Type T)
		{
			return "NG String_Test " + T.Name;
		}

//		public void DoTestClassTest<T>()
//		{
//			TestMethod(typeof(T));
//			TestMethod<T>();
//
//			Console.WriteLine(String_Test(typeof(T)));
//			Console.WriteLine(String_Test<T>());
//		}
//
		private void Test123(string a, int b, float c)
		{
		}
		public void DoTestClassTest_string()
		{
//			string test2 = (string)Test2Method<int, string>();
			Test123(" ", 123, 456.0f);

			TestVoid<string>();
			TestVoid(typeof(string));

			Type T = typeof(string);
			string tmng = (string)TestMethod(T);
			Console.WriteLine("TMNG " + tmng);
			string tm = TestMethod<string>();
			Console.WriteLine("TM " + tm);

//			string tmngS = (string)tmng;

//			Console.WriteLine(tmng + " " + tm);
			Console.WriteLine(String_Test(T));
			Console.WriteLine(String_Test<int>());
		}
	}
}

