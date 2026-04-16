using System.Collections.Generic;

namespace WutheringWaves
{
    public interface ICharacterStateMachineDriver
    {
        void RegisterSpecialStates(CharacterStateFactory factory, CharacterStateMachine machine);

        bool IsAttackable();
        bool IsAirAttackable();
        bool IsFallAttackable();
        bool IsESkillable();
        bool IsQBurstable();

        bool IsFloating { get; }

        List<AttackStep> AttackSteps { get; }
        List<AttackStep> FallAttackSteps { get; }
        List<AttackStep> SkillAttackSteps { get; }
        List<AttackStep> SkillAirAttackSteps { get; }
        List<AttackStep> ESkillAttackSteps { get; }
        List<AttackStep> QBurstAttackSteps { get; }

        AttackStep InitializeNormalAttackStep();
        AttackStep InitializeAirAttackStep(List<AttackStep> steps);
        AttackStep InitializeESkillStep();

        void StartNormalComboWindow();
        void StartAirComboWindow();
        void ResetNormalCombo();
        void ResetAirCombo();

        void OnSkill1Used();
        void OnSkill2Used();
        void OnSkill3Used();
        void OnSkill4Used();
        void OnQBurstUsed();
    }
}
