using HarmonyLib;

using BepInEx;
using BepInEx.Harmony;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using AIChara;
using Manager;

namespace AI_UnlockPlayerHClothes {
    [BepInPlugin(nameof(AI_UnlockPlayerHClothes), nameof(AI_UnlockPlayerHClothes), VERSION)][BepInProcess("AI-Syoujyo")]
    public class AI_UnlockPlayerHClothes : BaseUnityPlugin
    {
        public const string VERSION = "1.3.0";
        
        private static HScene hScene;
        private static HSceneManager manager;
        
        private static ChaControl player;
        private static readonly List<int> clothesKindList = new List<int>{0, 2, 1, 3, 5, 6};
        
        private void Awake() => HarmonyWrapper.PatchAll(typeof(AI_UnlockPlayerHClothes));

        private static ChaControl[] newGetFemales()
        {
            var females = hScene.GetFemales();
            var males = hScene.GetMales();

            return new ChaControl[4] { females[0], females[1], males[0], males[1] };
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(HSceneSprite), "OnClickMainCategories")]
        public static IEnumerable<CodeInstruction> HSceneSprite_OnClickMainCategories_AllowMalesClothesCategory(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            
            // Force show males selection in clothes category
            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Call && (instruction.operand as MethodInfo)?.Name == "SetAnimationMenu");
            if (index <= 0) return il;

            il[index + 5].opcode = OpCodes.Ldc_I4_2;
            il[index + 6].opcode = OpCodes.Clt;

           return il;
        }
        
        [HarmonyTranspiler, HarmonyPatch(typeof(HSceneSpriteClothCondition), "Init")]
        public static IEnumerable<CodeInstruction> HSceneSpriteClothCondition_Init_RedirectGetFemales(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            
            // Force clothes category to put males in "females" array
            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Callvirt && (instruction.operand as MethodInfo)?.Name == "GetFemales");
            if (index <= 0) return il;

            il[index - 2].opcode = OpCodes.Nop;
            il[index - 1].opcode = OpCodes.Nop;
            il[index] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AI_UnlockPlayerHClothes), nameof(newGetFemales)));
            
            return il;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HSceneSpriteClothCondition), "Init")]
        public static void HSceneSpriteClothCondition_Init_IncreaseAllState(HSceneSpriteClothCondition __instance)
        {
            // Force "all clothes off/on" to int array of 4 instead of 2
            var trav = Traverse.Create(__instance);
            trav.Field("allState").SetValue(new int[4]);
        }
        
        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "SetStartVoice")]
        public static void HScene_SetStartVoice_ApplyClothesConfig(HScene __instance)
        {
            hScene = __instance;
            
            var traverse = Traverse.Create(hScene);
            manager = traverse.Field("hSceneManager").GetValue<HSceneManager>();

            if(manager != null && manager.Player != null)
                player = manager.Player.ChaControl;

            var hData = Manager.Config.HData;
            foreach (var kind in clothesKindList.Where(kind => player.IsClothesStateKind(kind)))
                player.SetClothesState(kind, (byte)(hData.Cloth ? 0 : 2), true);
            
            player.SetAccessoryStateAll(hData.Accessory);
            player.SetClothesState(7, (byte)(!hData.Shoes ? 2 : 0), true);
        }
        
        [HarmonyTranspiler, HarmonyPatch(typeof(HScene), "LateUpdate")]
        public static IEnumerable<CodeInstruction> HScene_LateUpdate_RemoveClothesLock(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            
            // Force 0 to the if statement, clothes state doesn't get forced anymore //
            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Callvirt && (instruction.operand as MethodInfo)?.Name == "IsClothesStateKind");
            if (index <= 0) return il;

            il[index - 5].opcode = OpCodes.Nop;
            il[index - 4].opcode = OpCodes.Nop;
            il[index - 3].opcode = OpCodes.Nop;
            il[index - 2].opcode = OpCodes.Nop;
            il[index - 1].opcode = OpCodes.Nop;
            il[index].opcode = OpCodes.Ldc_I4_0;
            
            index = il.FindIndex(instruction => instruction.opcode == OpCodes.Callvirt && (instruction.operand as MethodInfo)?.Name == "SetAccessoryStateAll");
            if (index <= 0) return il;
            
            // Disable forcing accessory state //
            for (int i = 0; i < 7; i++)
                il[index - i].opcode = OpCodes.Nop;
            
            // Disable forcing shoe state //
            for (int i = 1; i < 15; i++)
                il[index + i].opcode = OpCodes.Nop;

            return il;
        }
    }
}
