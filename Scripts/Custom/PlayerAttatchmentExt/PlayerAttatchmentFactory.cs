using Server.Accounting;
using Server.Commands;
using Server.Commands.Generic;
using Server.ContextMenus;
using Server.Items;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;

namespace Server.Engines.PlayerAttatchmentExtensions
{
    public class PlayerAttatchmentFactory
    {
		private static char[] slashdelim = new char[1] { '/' };
		private static char[] commadelim = new char[1] { ',' };
		private static char[] spacedelim = new char[1] { ' ' };
		
		public static string[] ParseSlashArgs(string str, int nitems)
		{
			if (str == null) return null;

			string[] args = null;

			str = str.Trim();

			// this supports strings that may have special html formatting in them that use the /
			if (str.IndexOf("</") >= 0 || str.IndexOf("/>") >= 0)
			{
				// or use indexof to do it with more context control
				List<string> tmparray = new List<string>();
				// find the next slash char
				int index = 0;
				int preindex = 0;
				int searchindex = 0;
				int length = str.Length;
				while (index >= 0 && searchindex < length && tmparray.Count < nitems - 1)
				{
					index = str.IndexOf('/', searchindex);

					if (index >= 0)
					{
						// check the char before it and after it to ignore </ and />
						if ((index > 0 && str[index - 1] == '<') || (index < length - 1 && str[index + 1] == '>'))
						{
							// skip it
							searchindex = index + 1;
						}
						else
						{
							// split it
							tmparray.Add(str.Substring(preindex, index - preindex));

							preindex = index + 1;
							searchindex = preindex;
						}
					}

				}

				// is there still room for more args?
				if (tmparray.Count <= nitems - 1 && preindex < length)
				{
					// searched past the end and didnt find anything
					tmparray.Add(str.Substring(preindex, length - preindex));
				}

				// turn tmparray into a string[]

				args = new string[tmparray.Count];
				tmparray.CopyTo(args);
			}
			else
			{
				// just use split to do it with no context control
				args = str.Split(slashdelim, nitems);

			}

			return args;
		}
		public static string[] ParseSpaceArgs(string str, int nitems)
		{

			if (str == null) return null;

			string[] args = null;

			str = str.Trim();

			args = str.Split(spacedelim, nitems);

			return args;
		}


		public static string[] ParseCommaArgs(string str, int nitems)
		{
			if (str == null) return null;

			string[] args = null;

			str = str.Trim();

			args = str.Split(commadelim, nitems);

			return args;
		}
		public static string[] ParseObjectArgs(string str)
		{
			string[] arglist = ParseSlashArgs(str, 2);
			if (arglist != null && arglist.Length > 0)
			{
				string itemtypestring = arglist[0];
				// parse out any arguments of the form typename,arg,arg,..
				// find the first arg if it is there
				string[] typeargs = null;
				int argstart = 0;
				if (itemtypestring != null && itemtypestring.Length > 0)
					argstart = itemtypestring.IndexOf(",") + 1;

				if (argstart > 1 && argstart < itemtypestring.Length)
				{
					typeargs = ParseCommaArgs(itemtypestring.Substring(argstart), 15);
				}
				return (typeargs);

			}
			else
				return null;
		}
		
		private static bool IsConstructable(ConstructorInfo ctor)
        {
            return ctor.IsDefined(typeof(ConstructableAttribute), false);
        }
		

		#region Object Creation

		public static object CreateObjectExt(Type type, string itemtypestring)
		{
			return CreateObjectExt(type, itemtypestring, true);
		}

		public static object CreateObjectExt(Type type, string itemtypestring, bool requireconstructable)
		{
			// look for constructor arguments to be passed to it with the syntax type,arg1,arg2,.../
			string[] typewordargs = ParseObjectArgs(itemtypestring);

			return CreateObjectExt(type, typewordargs, requireconstructable, false);
		}

		public static object CreateObjectExt(Type type, string itemtypestring, bool requireconstructable, bool requireattachable)
		{
			// look for constructor arguments to be passed to it with the syntax type,arg1,arg2,.../
			string[] typewordargs = ParseObjectArgs(itemtypestring);

			return CreateObjectExt(type, typewordargs, requireconstructable, requireattachable);
		}

		public static object CreateObjectExt(Type type, string[] typewordargs, bool requireconstructable, bool requireattachable)
		{
			if (type == null) return null;

			object o = null;

			int typearglen = 0;
			if (typewordargs != null)
				typearglen = typewordargs.Length;

			// ok, there are args in the typename, so we need to invoke the proper constructor
			ConstructorInfo[] ctors = type.GetConstructors();

			if (ctors == null) return null;

			// go through all the constructors for this type
			for (int i = 0; i < ctors.Length; ++i)
			{
				ConstructorInfo ctor = ctors[i];

				if (ctor == null) continue;

				// if both requireconstructable and requireattachable are true, then allow either condition
				if (!(requireconstructable && IsConstructable(ctor)) && !(requireattachable && PlayerAttatchmentExt.IsAttachable(ctor)))
					continue;

				// check the parameter list of the constructor
				ParameterInfo[] paramList = ctor.GetParameters();

				// and compare with the argument list provided
				if (paramList != null && typearglen == paramList.Length)
				{
					// this is a constructor that takes args and matches the number of args passed in to CreateObjectExt
					if (paramList.Length > 0)
					{
						object[] paramValues = null;

						try
						{
							paramValues = Add.ParseValues(paramList, typewordargs);
						}
						catch { }

						if (paramValues == null)
							continue;

						// ok, have a match on args, so try to construct it
						try
						{
							o = Activator.CreateInstance(type, paramValues);
						}
						catch { }
					}
					else
					{
						// zero argument constructor
						try
						{
							o = Activator.CreateInstance(type);
						}
						catch { }
					}

					// successfully constructed the object, otherwise try another matching constructor
					if (o != null) break;
				}
			}

			return o;
		}

		#endregion

	}
	
}
