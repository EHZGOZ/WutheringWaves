using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    public class CharacterAttributes : MonoBehaviour
    {
        [Header("==核心组件==")]
        [SerializeField] private CharacterContext context; //角色共享上下文
        [SerializeField] private CharacterStateMachine stateMachine; //角色状态机
        [SerializeField] private CharacterRuntimeData runtimeData = new(); // 角色运行时数据

        public void Initialize(CharacterContext context)
        {
            this.context = context;
            stateMachine = context.StateMachine;
            runtimeData = context.RuntimeData;
        }
        
    }

}

