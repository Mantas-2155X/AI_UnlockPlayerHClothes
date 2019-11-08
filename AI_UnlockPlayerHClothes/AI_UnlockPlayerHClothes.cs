using HarmonyLib;

using BepInEx;
using BepInEx.Harmony;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using AIChara;
using Manager;

using JetBrains.Annotations;

namespace AI_UnlockPlayerHClothes {
    [BepInPlugin(nameof(AI_UnlockPlayerHClothes), nameof(AI_UnlockPlayerHClothes), "1.2.0")]
    public class AI_UnlockPlayerHClothes : BaseUnityPlugin
    {
        private static HScene hScene;
        private static HSceneManager manager;
        
        private static ChaControl player;
        private static readonly List<int> clothesKindList = new List<int>{0, 2, 1, 3, 5, 6};
        
        private void Awake() => HarmonyWrapper.PatchAll(typeof(AI_UnlockPlayerHClothes));

        private static ChaControl[] newGetFemales()
        {
            var females = hScene.GetFemales();
            
            if (females[1] != null || manager == null || manager.Player == null || manager.Player.ChaControl.sex != 0)
                return females;
            
            ChaControl[] newFemales = new ChaControl[2]
            {
                females[0],
                manager.Player.ChaControl
            };
            
            return newFemales;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(HSceneSpriteChaChoice), "Init")][UsedImplicitly]
        public static IEnumerable<CodeInstruction> HSceneSpriteChaChoice_Init_RedirectGetFemales(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            
            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Callvirt && (instruction.operand as MethodInfo)?.Name == "GetFemales");
            if (index <= 0) return il;

            il[index - 2].opcode = OpCodes.Nop;
            il[index - 1].opcode = OpCodes.Nop;
            il[index] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AI_UnlockPlayerHClothes), nameof(newGetFemales)));
            
            return il;
        }
        
        [HarmonyTranspiler, HarmonyPatch(typeof(HSceneSpriteAccessoryCondition), "Init")][UsedImplicitly]
        public static IEnumerable<CodeInstruction> HSceneSpriteAccessoryCondition_Init_RedirectGetFemales(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            
            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Callvirt && (instruction.operand as MethodInfo)?.Name == "GetFemales");
            if (index <= 0) return il;

            il[index - 2].opcode = OpCodes.Nop;
            il[index - 1].opcode = OpCodes.Nop;
            il[index] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AI_UnlockPlayerHClothes), nameof(newGetFemales)));
            
            return il;
        }
        
        [HarmonyTranspiler, HarmonyPatch(typeof(HSceneSpriteClothCondition), "Init")][UsedImplicitly]
        public static IEnumerable<CodeInstruction> HSceneSpriteClothCondition_Init_RedirectGetFemales(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            
            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Callvirt && (instruction.operand as MethodInfo)?.Name == "GetFemales");
            if (index <= 0) return il;

            il[index - 2].opcode = OpCodes.Nop;
            il[index - 1].opcode = OpCodes.Nop;
            il[index] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AI_UnlockPlayerHClothes), nameof(newGetFemales)));
            
            return il;
        }
        
        [HarmonyTranspiler, HarmonyPatch(typeof(HSceneSpriteCoordinatesCard), "Init")][UsedImplicitly]
        public static IEnumerable<CodeInstruction> HSceneSpriteCoordinatesCard_Init_RedirectGetFemales(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            
            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Callvirt && (instruction.operand as MethodInfo)?.Name == "GetFemales");
            if (index <= 0) return il;

            il[index - 2].opcode = OpCodes.Nop;
            il[index - 1].opcode = OpCodes.Nop;
            il[index] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AI_UnlockPlayerHClothes), nameof(newGetFemales)));
            
            return il;
        }
        
        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "SetStartVoice")][UsedImplicitly]
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
        
        [HarmonyTranspiler, HarmonyPatch(typeof(HScene), "LateUpdate")][UsedImplicitly]
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

            // Disable forcing accessory & shoe state //
            index = il.FindIndex(instruction => instruction.opcode == OpCodes.Callvirt && (instruction.operand as MethodInfo)?.Name == "SetAccessoryStateAll");
            if (index <= 0) return il;
            
            for (int i = -6; i < 22; i++)
                il[index + i].opcode = OpCodes.Nop;

            return il;
        }
    }
}
