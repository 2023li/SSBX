using UnityEngine;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>
    /// 建筑信息面板基类：所有具体建筑信息UI都应继承它。
    /// </summary>
    public abstract class BuildingPanelBase : MonoBehaviour
    {
        [ReadOnly, LabelText("绑定建筑")] public Building bound;

        /// <summary>打开面板并绑定数据。</summary>
        public void Open(Building b)
        {
            bound = b;
            gameObject.SetActive(true);
            OnOpen(b);
        }

        /// <summary>关闭面板。</summary>
        public virtual void Close()
        {
            gameObject.SetActive(false);
        }

        /// <summary>子类填充UI。</summary>
        protected abstract void OnOpen(Building b);
    }
}
