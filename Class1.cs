using HarmonyLib;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections;
using System.Reflection;
using System.Drawing;
using PotionCraft.ObjectBased.UIElements.InventoryWindow;

namespace ukersn
{
    [BepInPlugin("com.ukersn.potioncraftmods.recipebutton", "RecipeButtonFix", "1.0.0")]
    public class RecipeButtonFixPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log = null;
        private Harmony harmony = null;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo("配方按钮修复插件正在加载...");

            StartCoroutine(InitializePlugin());
        }

        private IEnumerator InitializePlugin()
        {
            yield return new WaitForSeconds(10f);

            try
            {
                harmony = new Harmony("com.ukersn.potioncraftmods.recipebutton");
                ApplyPatches();
                Log.LogInfo("配方按钮修复插件已成功加载！");
            }
            catch (Exception ex)
            {
                Log.LogError(string.Format("插件初始化时发生错误: {0}", ex.Message));
                Log.LogError(ex.StackTrace);
            }
        }

        private void ApplyPatches()
        {
            try
            {
                harmony = new Harmony("com.ukersn.potioncraftmods.recipebutton");

                // ShowSavedRecipes 补丁
                MethodInfo originalShowSavedRecipes = AccessTools.Method(typeof(OpenRecipeBookButton), "ShowSavedRecipes");
                MethodInfo postfixShowSavedRecipes = AccessTools.Method(typeof(OpenRecipeBookButtonPatch), "PostShowSavedRecipes");
                harmony.Patch(originalShowSavedRecipes, postfix: new HarmonyMethod(postfixShowSavedRecipes));

                Log.LogInfo("成功应用 RecipeButtonFix 补丁");
            }
            catch (Exception ex)
            {
                Log.LogError(string.Format("应用补丁时发生错误: {0}", ex.Message));
                Log.LogError(ex.StackTrace);
            }
        }


        private void OnDisable()
        {
            try
            {
                harmony?.UnpatchSelf();
                Log.LogInfo("配方按钮修复插件已成功卸载所有补丁。");
            }
            catch (Exception ex)
            {
                Log.LogError(string.Format("卸载补丁时发生错误: {0}", ex.Message));
            }
        }
    }
    
    [HarmonyPatch(typeof(OpenRecipeBookButton))]
    public static class OpenRecipeBookButtonPatch
    {
        private static Vector2 originalColliderSize=Vector2.zero;

        [HarmonyPostfix]
        [HarmonyPatch("ShowSavedRecipes")]
        public static void PostShowSavedRecipes(OpenRecipeBookButton __instance)
        {
            //RecipeButtonFixPlugin.Log.LogInfo(string.Format("检查ShowSavedRecipes"));
            ResetColliderSize(__instance);
        }

        private static void ResetColliderSize(OpenRecipeBookButton button)
        {
            try
            {
                BoxCollider2D collider = button.GetComponent<BoxCollider2D>();
                //RecipeButtonFixPlugin.Log.LogInfo(string.Format("检查ResetColliderSize此时按钮大小 :{0}", collider.size));
                if (originalColliderSize == Vector2.zero) {
                    //RecipeButtonFixPlugin.Log.LogInfo(string.Format("检查到按钮碰撞器大小未初始化"));
                    originalColliderSize = collider.size;
                }
                else if (collider != null && collider.size != originalColliderSize)
                {
                    collider.size = originalColliderSize;
                    RecipeButtonFixPlugin.Log.LogInfo(string.Format("Reset RecipeButton collider size to: {0}", originalColliderSize));
                }
            }
            catch (Exception ex)
            {
                RecipeButtonFixPlugin.Log.LogError(string.Format("Error in ResetColliderSize: {0}", ex.Message));
            }
        }
    }
}
