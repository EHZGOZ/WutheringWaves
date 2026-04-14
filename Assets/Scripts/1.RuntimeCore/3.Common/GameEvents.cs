using System;

namespace WutheringWaves
{
    /// <summary>
    /// Global event bus for cross-module communication.
    /// </summary>
    public static class GameEvents
    {
        // source, oldState, newState
        public static event Action<CharacterStateMachine, CharacterState, CharacterState> OnCharacterStateChanged;

        // source, isFloating
        public static event Action<CharacterAttack, bool> OnFloatingChanged;

        // source
        public static event Action<CharacterAttack> OnSkillUIStateChanged;

        // source, current, max, normalized
        public static event Action<CharacterStamina, float, float, float> OnStaminaChanged;

        // source, visible
        public static event Action<CharacterStamina, bool> OnStaminaVisibilityChanged;

        public static void RaiseCharacterStateChanged(CharacterStateMachine source, CharacterState oldState, CharacterState newState)
        {
            OnCharacterStateChanged?.Invoke(source, oldState, newState);
        }

        public static void RaiseFloatingChanged(CharacterAttack source, bool isFloating)
        {
            OnFloatingChanged?.Invoke(source, isFloating);
        }

        public static void RaiseSkillUIStateChanged(CharacterAttack source)
        {
            OnSkillUIStateChanged?.Invoke(source);
        }

        public static void RaiseStaminaChanged(CharacterStamina source, float current, float max, float normalized)
        {
            OnStaminaChanged?.Invoke(source, current, max, normalized);
        }

        public static void RaiseStaminaVisibilityChanged(CharacterStamina source, bool visible)
        {
            OnStaminaVisibilityChanged?.Invoke(source, visible);
        }
    }
}
