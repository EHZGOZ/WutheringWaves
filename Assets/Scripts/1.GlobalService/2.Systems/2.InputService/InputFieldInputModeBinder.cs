using TMPro;
using UnityEngine;

namespace WutheringWaves
{
    // 文本输入框输入模式绑定：负责在输入框选中和结束输入时通知InputService切换输入状态
    [RequireComponent(typeof(TMP_InputField))]
    public class InputFieldInputModeBinder : MonoBehaviour
    {
        [Header(" 输入框")]
        [SerializeField] private TMP_InputField inputField;

        [Header(" 结束输入后是否恢复玩家输入")]
        [SerializeField] private bool restorePlayerInputOnEnd;

        #region 生命周期
        private void Awake()
        {
            // 1.自动获取当前物体上的TMP输入框
            if (inputField == null)
            {
                inputField = GetComponent<TMP_InputField>();
            }
        }

        private void OnEnable()
        {
            // 1.绑定输入框事件
            BindEvents();
        }

        private void OnDisable()
        {
            // 1.解绑输入框事件，避免对象禁用后残留监听
            UnbindEvents();
        }
        #endregion

        #region 事件绑定
        private void BindEvents()
        {
            if (inputField == null)
            {
                return;
            }

            inputField.onSelect.AddListener(HandleInputSelected);
            inputField.onDeselect.AddListener(HandleInputDeselected);
            inputField.onEndEdit.AddListener(HandleInputEndEdit);
        }

        private void UnbindEvents()
        {
            if (inputField == null)
            {
                return;
            }

            inputField.onSelect.RemoveListener(HandleInputSelected);
            inputField.onDeselect.RemoveListener(HandleInputDeselected);
            inputField.onEndEdit.RemoveListener(HandleInputEndEdit);
        }
        #endregion

        #region 输入框事件
        // 输入框被选中：进入文本输入模式
        private void HandleInputSelected(string value)
        {
            InputService.Instance?.BeginTextInput();
        }

        // 输入框失去焦点：退出文本输入模式
        private void HandleInputDeselected(string value)
        {
            InputService.Instance?.EndTextInput(restorePlayerInputOnEnd);
        }

        // 输入框结束编辑：退出文本输入模式
        private void HandleInputEndEdit(string value)
        {
            InputService.Instance?.EndTextInput(restorePlayerInputOnEnd);
        }
        #endregion
    }
}