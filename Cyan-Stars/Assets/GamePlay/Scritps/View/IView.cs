using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 视图层接口
/// </summary>
public interface IView
{
    void OnUpdate(float deltaTime);

    void DestorySelf(bool autoMove = true);
    bool IsTiggered();
    Transform GetTransform();
}