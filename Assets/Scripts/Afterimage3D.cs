using System;
using UnityEngine;

namespace WutheringWaves
{
    // 3D残影效果核心类：挂载到需要生成残影的对象上，生成视觉残影并自动销毁
    public class Afterimage3D : MonoBehaviour
    {
        // 存储残影克隆体的数组（每个渲染器对应一个克隆对象）
        private GameObject[] clones;
        // 残影的生命周期（单位：秒），到时间后销毁残影
        public float lifetime = 1f;

        #region 生命周期函数
        // 生成残影克隆体
        void Awake()
        {
            // 获取当前对象及所有子对象的渲染器组件（true表示包含非激活对象）
            Renderer[] renders = GetComponentsInChildren<Renderer>(true);
            // 先计算需要激活的残影数量（仅处理激活状态的渲染器对应的对象）
            int activeCount = 0;
            // 遍历所有渲染器，统计激活状态的数量
            foreach (var render in renders)
            {
                // 检查渲染器所在的游戏对象是否在层级中激活
                if (render.gameObject.activeInHierarchy)
                {
                    activeCount++;
                }
            }

            // 初始化克隆体数组，长度为激活的渲染器数量
            clones = new GameObject[activeCount];
            // 克隆体数组的索引计数器
            int cloneIndex = 0;

            // 遍历所有渲染器，为每个激活的渲染器创建残影克隆体
            for (var i = 0; i < renders.Length; i++)
            {
                // 当前遍历到的渲染器
                var renderer = renders[i];

                // 如果渲染器所在对象未激活，则跳过该渲染器（不生成对应残影）
                if (!renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                // 创建新的游戏对象作为残影克隆体，命名规则：Afterimage_索引_原对象名（便于调试）
                var go = new GameObject($"Afterimage_{i}_{renderer.gameObject.name}");
                // 将新建的克隆体存入数组
                clones[cloneIndex] = go;
                // 索引计数器自增
                cloneIndex++;

                // 为克隆体添加网格渲染器组件（用于显示残影）
                var cloneRenderer = go.AddComponent<MeshRenderer>();
                // 为克隆体赋值独立的材质实例（避免与原对象共享材质导致视觉异常）
                cloneRenderer.materials = CloneMaterials(renderer.materials);

                // 声明网格变量：存储克隆体需要显示的网格数据
                Mesh mesh = null;
                // 判断当前渲染器是否为骨骼网格渲染器（人物/动画对象常用）
                if (renderer is SkinnedMeshRenderer skinnedRenderer)
                {
                    // 为骨骼网格创建新的网格实例（避免共享原网格数据）
                    mesh = new Mesh();
                    // 将骨骼网格烘焙为静态网格（true表示使用世界空间坐标），用于残影显示
                    skinnedRenderer.BakeMesh(mesh, true);
                }
                // 判断当前渲染器是否为普通网格渲染器（静态物体常用）
                else if (renderer is MeshRenderer)
                {
                    // 获取网格过滤器组件（MeshFilter存储网格数据）
                    var filter = renderer.GetComponent<MeshFilter>();
                    // 检查过滤器和共享网格是否存在（避免空引用）
                    if (filter != null && filter.sharedMesh != null)
                    {
                        // 关键修复：使用sharedMesh而不是实例化（减少内存占用）
                        mesh = filter.sharedMesh;
                    }
                }

                // 如果网格数据不为空，则为克隆体赋值网格并同步变换信息
                if (mesh != null)
                {
                    // 为克隆体添加网格过滤器，并赋值网格数据
                    go.AddComponent<MeshFilter>().mesh = mesh;

                    // 同步克隆体的位置（与原渲染器所在对象位置一致）
                    go.transform.position = renderer.transform.position;
                    // 同步克隆体的旋转（与原渲染器所在对象旋转一致）
                    go.transform.rotation = renderer.transform.rotation;
                    // 同步克隆体的缩放（使用lossyScale：世界空间下的缩放，避免父对象缩放影响）
                    go.transform.localScale = renderer.transform.lossyScale;
                }
                else
                {
                    // 如果无法获取网格，禁用该克隆体（避免空对象占用资源）
                    go.SetActive(false);
                }
            }

        }
        // 开始函数：在Awake后、第一帧更新前执行，主要做初始化检查
        private void Start()
        {
            // 1. 检查单例是否存在（防御性编程）
            if (SoundManager.Instance == null)
            {
                // 打印错误日志：提示SoundManager单例未初始化（便于调试定位问题）
                Debug.LogError("❌ SoundManager 单例未初始化！请确保场景中存在激活的 SoundManager 对象。");
                // 终止后续逻辑（避免空引用错误）
                return;
            }
        }

        // 更新函数：每帧执行，核心逻辑是倒计时并销毁残影
        void Update()
        {
            // 每帧减少生命周期（Time.deltaTime：上一帧的耗时，保证时间流逝与帧率无关）
            lifetime -= Time.deltaTime;
            // 如果生命周期小于等于0，销毁当前脚本所在对象（即销毁残影）
            if (lifetime <= 0)
                Destroy(this);
        }

        // 销毁函数：在对象被销毁时执行，核心逻辑是清理克隆体和网格资源（避免内存泄漏）
        private void OnDestroy()
        {
            // 如果克隆体数组为空，直接返回（避免空引用）
            if (clones == null) return;

            // 遍历所有克隆体，逐个清理资源
            foreach (var go in clones)
            {
                // 检查克隆体是否存在（避免重复销毁）
                if (go != null)
                {
                    // 获取克隆体的网格过滤器组件
                    var meshFilter = go.GetComponent<MeshFilter>();
                    // 只销毁动态创建的骨骼网格（判断网格名称包含"BakedMesh"）
                    if (meshFilter != null && meshFilter.mesh != null &&
                       meshFilter.mesh.name.Contains("BakedMesh"))
                    {
                        // 销毁动态创建的网格（释放内存）
                        Destroy(meshFilter.mesh);
                    }
                    // 销毁克隆体游戏对象
                    Destroy(go);
                }
            }
            // 将克隆体数组置空（避免野指针）
            clones = null;
        }

        #endregion

        // 工具方法：克隆材质数组，创建独立的材质实例（避免原对象材质修改影响残影）
        private Material[] CloneMaterials(Material[] originalMaterials)
        {
            // 初始化新材质数组，长度与原数组一致
            Material[] newMaterials = new Material[originalMaterials.Length];
            // 遍历原材质数组，逐个克隆
            for (int i = 0; i < originalMaterials.Length; i++)
            {
                // 创建原材质的新实例（独立引用，不共享数据）
                newMaterials[i] = new Material(originalMaterials[i])
                {
                    // 确保材质使用正确的着色器（通过名称查找，避免着色器引用丢失）
                    shader = Shader.Find(originalMaterials[i].shader.name)
                };
            }
            // 返回克隆后的材质数组
            return newMaterials;
        }

    }

}
