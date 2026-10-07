using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI/CircleMask 셰이더가 쓸 좌표를 채워 넣는 Image.
///
/// 셰이더가 원을 그리려면 "이 칸의 한가운데가 어디인지"를 알아야 하는데,
/// 스프라이트 UV로는 알 수 없다. 아이콘 PNG를 임포트할 때 가장자리 여백이
/// 잘려 들어가면 UV가 0~1이 아니라 0.02~0.98 같은 값이 되고, 그러면 원이
/// 한쪽으로 치우친다. 실제로 유물 아이콘들이 그렇게 들어와 있다.
///
/// 그래서 사각형 기준 0~1 좌표를 TEXCOORD1에 직접 적어준다.
/// 셰이더를 안 쓰면 이 값은 그냥 무시되므로, 평소 Image와 똑같이 동작한다.
/// </summary>
[AddComponentMenu("UI/Circle Masked Image")]
public class CircleMaskedImage : Image
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        base.OnPopulateMesh(vh);

        Rect rect = GetPixelAdjustedRect();

        if (rect.width <= 0f || rect.height <= 0f) return;

        UIVertex vertex = new UIVertex();

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);

            vertex.uv1 = new Vector2(
                (vertex.position.x - rect.xMin) / rect.width,
                (vertex.position.y - rect.yMin) / rect.height);

            vh.SetUIVertex(vertex, i);
        }
    }
}
