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

        private bool isTextInputBound; // 当前输入框是否已经进入文本输入模式

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
            // 1.禁用输入框时，兜底退出文本输入模式
            EndTextInputMode();

            // 2.解绑输入框事件，避免对象禁用后残留监听
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

        #region 进入文本输入模式
        // 输入框被选中：进入文本输入模式
        private void HandleInputSelected(string value)
        {
            // 1.已经进入文本输入模式时不重复处理
            if (isTextInputBound)
            {
                return;
            }

            // 2.标记当前输入框已经接管文本输入
            isTextInputBound = true;

            // 3.通知输入服务进入文本输入模式
            InputService.Instance?.BeginTextInput();
        }

        #endregion

        #region 退出文本输入模式
        // 输入框失去焦点：退出文本输入模式
        private void HandleInputDeselected(string value)
        {
            EndTextInputMode();
        }

        // 输入框结束编辑：退出文本输入模式
        private void HandleInputEndEdit(string value)
        {
            EndTextInputMode();
        }
        // 退出文本输入模式：统一处理onDeselect和onEndEdit可能重复触发的问题
        private void EndTextInputMode()
        {
            // 1.当前输入框没有接管文本输入时，不重复退出
            if (!isTextInputBound)
            {
                return;
            }

            // 2.先清空标记，避免onDeselect和onEndEdit重复触发时重复处理
            isTextInputBound = false;

            // 3.通知输入服务退出文本输入模式
            InputService.Instance?.EndTextInput(restorePlayerInputOnEnd);
        }
        #endregion

        #endregion


    }
}