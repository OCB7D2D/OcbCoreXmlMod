/* MIT License

Copyright (c) 2022 OCB7D2D

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using HarmonyLib;

static class ModXmlConditions
{

    private static readonly string NameAssembly = "ConditionalXml";
    private static readonly string NameModule = "ConditionalXml";
    private static readonly string NameType = "ConditionalXml";
    private static readonly string NameField = "XmlConditions";

    private static Dictionary<string, Func<bool>> GetDynamicXmlConditions()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic && assembly.GetName().Name == NameAssembly)
            {
                if (assembly.GetType(NameType) is Type klass)
                {
                    if (klass.GetField(NameField) is FieldInfo field)
                    {
                        if (field.GetValue(klass) is Dictionary<string, Func<bool>> conditions)
                        {
                            Log.Warning("Found the field too");
                            return conditions;
                        }
                    }
                }
            }
        }
        return null;
    }

    // Some out of the box checks we easily can provide
    private static bool isSmxLoaded = (AccessTools.TypeByName("Quartz.ItemStack") != null);

    private static bool IsSmxLoaded() => isSmxLoaded;

    private static void AddOurConditions(Dictionary<string, Func<bool>> conditions)
    {
        if (!conditions.ContainsKey("smx")) conditions.Add("smx", IsSmxLoaded);
    }

    public static Dictionary<string, Func<bool>> CreateOrLoadConditionalXML()
    {

        // Try to find dynamically and bail if OK
        // Ensures we only create this once globally
        if (GetDynamicXmlConditions() is Dictionary<string, Func<bool>> conditions)
        {
            AddOurConditions(conditions);
            return conditions;
        }

        // Dynamically create a new assembly on the fly
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
           new AssemblyName(NameAssembly),
           AssemblyBuilderAccess.Run);

        // Create one module inside the assembly
        ModuleBuilder module = assembly.DefineDynamicModule(NameModule);

        // Create one class inside the module
        TypeBuilder klass = module.DefineType(NameType,
            TypeAttributes.Public | TypeAttributes.Class);

        // Create one field inside the static class
        FieldBuilder field = klass.DefineField(NameField,
            typeof(Dictionary<string, Func<bool>>),
            FieldAttributes.Static | FieldAttributes.Public);

        // Create a method to initialize the field
        // Crutch since we seem unable to call `SetValue`
        var initMethod = klass.DefineMethod("Initialize",
            MethodAttributes.Static | MethodAttributes.Private,
            CallingConventions.Standard,
            typeof(void),
            new[] { typeof(Dictionary<string, Func<bool>>) });
        var il = initMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Stsfld, field);
        il.Emit(OpCodes.Ret);

        // Finally bake the whole thing
        Type type = klass.CreateType();

        // Create the "one and only" dictionary now
        conditions = new Dictionary<string, Func<bool>>();

        // Add initial OOTB checks 
        AddOurConditions(conditions);

        // Dynamically execute newly created method to assign Dictionary instance
        type.GetMethod("Initialize", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { conditions });

        return conditions;
    }

}
