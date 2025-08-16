using UnityEngine;

namespace BoxCC
{
    // GroundEffector 클래스는 캐릭터가 특정 지면에 닿았을 때
    // 캐릭터의 움직임에 변화를 주는 역할을 합니다.
    // 예를 들어, 계단을 오르거나 사다리를 타고 올라가는 움직임을
    // 이 스크립트를 통해 정의할 수 있습니다.
    public class GroundEffector : MonoBehaviour
    {
        // 인스펙터에서 설정 가능한 2x2 행렬(Vector4)입니다.
        // 이 행렬을 사용하여 캐릭터의 이동 벡터를 변형(transform)합니다.
        // Vector4의 (x, y, z, w)는 행렬의 (m11, m21, m12, m22)에 대응합니다.
        [SerializeField] private Vector4 _groundEffectorMatrix = new Vector4(1, 0, 0, 1);

        // 외부에서 이 행렬 값에 접근할 수 있도록 하는 속성(Property)입니다.
        // '=>'는 읽기 전용 속성을 간결하게 정의하는 문법입니다.
        public Vector4 Matrix => _groundEffectorMatrix;
    }
}