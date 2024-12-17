using System;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            string dllPath = @"F:\SteamLibrary\steamapps\common\Potion Craft\MYdll\PotionCraft.Scripts.dll";
            string outputPath = @"F:\SteamLibrary\steamapps\common\Potion Craft\Potion Craft_Data\Managed\PotionCraft.Scripts.dll";

            ModuleContext modCtx = ModuleDef.CreateModuleContext();
            ModuleDefMD module = ModuleDefMD.Load(dllPath, modCtx);

            module.Import(typeof(UnityEngine.Vector2));
            module.Import(typeof(UnityEngine.BoxCollider2D));
            module.Import(typeof(UnityEngine.Component));
            //module.Import(typeof(UnityEngine.Debug));
            // 查找 OpenRecipeBookButton 类
            TypeDef openRecipeBookButtonType = module.Find("PotionCraft.ObjectBased.UIElements.InventoryWindow.OpenRecipeBookButton", false);
            if (openRecipeBookButtonType == null)
            {
                Console.WriteLine("未找到 OpenRecipeBookButton 类");
                return;
            }

            // 添加 originalColliderSizeX 和 originalColliderSizeY 字段
            FieldDefUser originalColliderSizeX = new FieldDefUser(
                "originalColliderSizeX",
                new FieldSig(module.CorLibTypes.Single),
                FieldAttributes.Private | FieldAttributes.Static
            );
            FieldDefUser originalColliderSizeY = new FieldDefUser(
                "originalColliderSizeY",
                new FieldSig(module.CorLibTypes.Single),
                FieldAttributes.Private | FieldAttributes.Static
            );
            openRecipeBookButtonType.Fields.Add(originalColliderSizeX);
            openRecipeBookButtonType.Fields.Add(originalColliderSizeY);

            // 查找 ShowSavedRecipes 方法
            MethodDef showSavedRecipesMethod = openRecipeBookButtonType.Methods.FirstOrDefault(m => m.Name == "ShowSavedRecipes");
            if (showSavedRecipesMethod == null)
            {
                Console.WriteLine("未找到 ShowSavedRecipes 方法");
                return;
            }

            // 创建 ResetColliderSize 方法
            MethodDef resetColliderSizeMethod = CreateResetColliderSizeMethod(module, openRecipeBookButtonType, originalColliderSizeX, originalColliderSizeY);

            // 修改 ShowSavedRecipes 方法
            ModifyShowSavedRecipesMethod(showSavedRecipesMethod, resetColliderSizeMethod);

            // 保存修改后的 DLL
            module.Write(outputPath);
            Console.WriteLine("DLL 修改完成，已保存到 " + outputPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine("发生错误: " + ex.Message);
            Console.WriteLine("堆栈跟踪: " + ex.StackTrace);
        }
    }

    static MethodDef CreateResetColliderSizeMethod(ModuleDefMD module, TypeDef classType, FieldDef originalColliderSizeX, FieldDef originalColliderSizeY)
    {
        MethodDef method = new MethodDefUser(
            "ResetColliderSize",
            MethodSig.CreateInstance(module.CorLibTypes.Void),
            MethodAttributes.Private
        );

        classType.Methods.Add(method);

        CilBody body = new CilBody();
        method.Body = body;

        // 添加本地变量
        Local colliderLocal = new Local(module.Import(typeof(UnityEngine.BoxCollider2D)).ToTypeSig());
        body.Variables.Add(colliderLocal);

        // 获取 BoxCollider2D 组件
        body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
        body.Instructions.Add(OpCodes.Call.ToInstruction(module.Import(typeof(UnityEngine.Component).GetMethod("GetComponent", new Type[] { }).MakeGenericMethod(typeof(UnityEngine.BoxCollider2D)))));
        body.Instructions.Add(OpCodes.Stloc_0.ToInstruction());

        // 检查 collider 是否为 null
        body.Instructions.Add(OpCodes.Ldloc_0.ToInstruction());
        Instruction returnInstruction = OpCodes.Ret.ToInstruction();
        body.Instructions.Add(OpCodes.Brfalse.ToInstruction(returnInstruction));

        //// 输出当前碰撞器大小
        //body.Instructions.Add(OpCodes.Ldstr.ToInstruction("Current collider size: {0}"));
        //body.Instructions.Add(OpCodes.Ldc_I4_1.ToInstruction()); // 参数数量为 1
        //body.Instructions.Add(OpCodes.Newarr.ToInstruction(module.CorLibTypes.Object)); // 创建对象数组
        //body.Instructions.Add(OpCodes.Dup.ToInstruction());
        //body.Instructions.Add(OpCodes.Ldc_I4_0.ToInstruction()); // 数组索引 0
        //body.Instructions.Add(OpCodes.Ldloc_0.ToInstruction());
        //body.Instructions.Add(OpCodes.Callvirt.ToInstruction(module.Import(typeof(UnityEngine.BoxCollider2D).GetProperty("size").GetGetMethod())));
        //body.Instructions.Add(OpCodes.Box.ToInstruction(module.Import(typeof(UnityEngine.Vector2)))); // 装箱 Vector2
        //body.Instructions.Add(OpCodes.Stelem_Ref.ToInstruction());
        //body.Instructions.Add(OpCodes.Call.ToInstruction(module.Import(typeof(UnityEngine.Debug).GetMethod("LogFormat", new[] { typeof(string), typeof(object[]) }))));

        // 如果 originalColliderSizeX 为零，设置原始大小
        body.Instructions.Add(OpCodes.Ldsfld.ToInstruction(originalColliderSizeX));
        body.Instructions.Add(OpCodes.Ldc_R4.ToInstruction(0f));
        body.Instructions.Add(OpCodes.Ceq.ToInstruction());
        Instruction skipSetOriginalSize = OpCodes.Nop.ToInstruction();
        body.Instructions.Add(OpCodes.Brfalse.ToInstruction(skipSetOriginalSize));

        body.Instructions.Add(OpCodes.Ldloc_0.ToInstruction());
        body.Instructions.Add(OpCodes.Callvirt.ToInstruction(module.Import(typeof(UnityEngine.BoxCollider2D).GetProperty("size").GetGetMethod())));
        body.Instructions.Add(OpCodes.Dup.ToInstruction());
        body.Instructions.Add(OpCodes.Ldfld.ToInstruction(module.Import(typeof(UnityEngine.Vector2).GetField("x"))));
        body.Instructions.Add(OpCodes.Stsfld.ToInstruction(originalColliderSizeX));
        body.Instructions.Add(OpCodes.Ldfld.ToInstruction(module.Import(typeof(UnityEngine.Vector2).GetField("y"))));
        body.Instructions.Add(OpCodes.Stsfld.ToInstruction(originalColliderSizeY));

        body.Instructions.Add(skipSetOriginalSize);

        // 重置 collider size
        body.Instructions.Add(OpCodes.Ldloc_0.ToInstruction());
        body.Instructions.Add(OpCodes.Ldsfld.ToInstruction(originalColliderSizeX));
        body.Instructions.Add(OpCodes.Ldsfld.ToInstruction(originalColliderSizeY));
        body.Instructions.Add(OpCodes.Newobj.ToInstruction(module.Import(typeof(UnityEngine.Vector2).GetConstructor(new[] { typeof(float), typeof(float) }))));
        body.Instructions.Add(OpCodes.Callvirt.ToInstruction(module.Import(typeof(UnityEngine.BoxCollider2D).GetProperty("size").GetSetMethod())));

        body.Instructions.Add(returnInstruction);

        return method;
    }

    static void ModifyShowSavedRecipesMethod(MethodDef method, MethodDef resetColliderSizeMethod)
    {
        CilBody body = method.Body;
        if (body == null)
        {
            Console.WriteLine("ShowSavedRecipes 方法没有方法体");
            return;
        }

        // 在方法开始处添加对 ResetColliderSize 的调用
        body.Instructions.Insert(0, OpCodes.Ldarg_0.ToInstruction());
        body.Instructions.Insert(1, OpCodes.Call.ToInstruction(resetColliderSizeMethod));
    }
}