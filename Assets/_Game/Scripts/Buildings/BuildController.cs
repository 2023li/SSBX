
using UnityEngine;
using UnityEngine.InputSystem;

namespace SSBX
{
    /// <summary>
    /// 建造流程控制（简版）：点击UI进入“放置模式”，鼠标左键选格，右键/ESC取消。
    /// Ghost与区域高亮可先用一个带透明材质的方块占位。
    /// </summary>
    public class BuildController : MonoBehaviour
    {
        public Building buildingPrefab;     // 任意建筑的预制体（在UI里切换赋值）
        public BuildingConfig buildingConfig;

        [Header("Runtime")]
        public bool placing;
        public Vector3Int hoverCell;

        private Camera _cam;

        private void Start()
        {
            _cam = Camera.main;
        }

        private void Update()
        {
            if (!placing) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            // ✅ 正确的屏幕→世界坐标换算，显式指定与相机的距离，锁定Z=0
            var mp = mouse.position.ReadValue();
            var screen = new Vector3(mp.x, mp.y, -_cam.transform.position.z);
            var world = _cam.ScreenToWorldPoint(screen);
            world.z = 0f; // ✅ 世界Z=0

            if (GridSystem.Instance.TryGetCell(world, out hoverCell))
            {
                // TODO: Ghost 的可视化（里程碑2处理）
            }

            if (mouse.leftButton.wasPressedThisFrame)
                TryConfirmPlace();

            //if (mouse.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
            //    CancelPlace();
        }

        public void EnterPlaceMode(Building prefab, BuildingConfig cfg)
        {
            buildingPrefab = prefab;
            buildingConfig = cfg;
            placing = true;
        }

        private void TryConfirmPlace()
        {
            if (!placing) return;
            if (!BuildingManager.Instance.CanPlace(buildingConfig, hoverCell))
            {
                Debug.Log("此处不可放置");
                return;
            }
            var b = BuildingManager.Instance.Place(buildingPrefab, buildingConfig, hoverCell);
            Debug.Log($"放置建筑：{b.config.displayName} at {hoverCell}");
            placing = false;
        }

        private void CancelPlace()
        {
            placing = false;
        }
    }
}

