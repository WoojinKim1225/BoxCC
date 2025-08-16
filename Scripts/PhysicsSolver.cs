using UnityEngine;

namespace BoxCC
{
    // PhyscisSolver 클래스는 물리 계산을 수행하는 정적(static) 클래스입니다.
    // MonoBehaviour를 상속받지 않으므로, GameObject에 컴포넌트로 추가할 수 없습니다.
    public static class PhyscisSolver
    {
        /// <summary>
        /// Kinematic Rigidbody 충돌 연산 함수입니다.
        /// 벽이나 지면에 부딪혔을 때 튕겨 나가거나 미끄러지도록 움직임을 계산합니다.
        /// </summary>
        /// <param name="dp"> dt(고정 업데이트 시간) 동안의 변위(이동량)입니다. </param>
        /// <param name="p"> Rigidbody의 현재 위치입니다. </param>
        /// <param name="skinWidth"> 충돌 감지를 위한 콜라이더의 스킨 폭입니다. </param>
        /// <param name="collider_size"> 콜라이더의 원래 크기입니다. </param>
        /// <param name="whatIsWall"> 벽으로 간주할 레이어 마스크입니다. </param>
        /// <param name="whatIsGroundEffector"> 지면 효과(경사로 등)로 간주할 레이어 마스크입니다. </param>
        /// <param name="depth"> 재귀 호출 깊이를 추적하는 변수입니다. 무한 루프 방지용. </param>
        /// <returns> 계산된 최종 변위(이동량)를 반환합니다. </returns>
        public static Vector2 MoveAndSlide(Vector2 dp, Vector2 p, in float skinWidth, in Vector2 collider_size, in LayerMask whatIsWall, in LayerMask whatIsGroundEffector, int depth = 0)
        {
            // 재귀 호출이 너무 많이 발생하면 무한 루프에 빠지지 않도록 연산을 중단합니다.
            if (depth >= 3) return Vector2.zero;

            // 충돌 감지를 위해 콜라이더의 크기를 스킨 폭만큼 줄입니다.
            // 이는 좁은 틈에 끼이는 현상을 방지하는 데 도움을 줍니다.
            Vector2 size = collider_size - Vector2.one * skinWidth * 2f;

            // 이동 거리를 계산합니다.
            float l = dp.magnitude + skinWidth;

            // depth가 0일 때, 즉 최초 호출 시에만 지면 효과(GroundEffector)를 먼저 확인합니다.
            if (depth == 0)
            {
                // 1. 이동 방향으로 GroundEffector를 확인합니다.
                RaycastHit2D h1 = Physics2D.Raycast(p, dp.normalized, dp.magnitude, whatIsGroundEffector);
                if (h1.collider != null && h1.fraction != 0)
                {
                    // GroundEffector 컴포넌트를 가져옵니다.
                    Debug.Assert(h1.collider.TryGetComponent<GroundEffector>(out GroundEffector effector));
                    // 지면 효과와 충돌하기 전까지 이동하고, 그 이후의 움직임을 재귀 호출로 계산합니다.
                    return MoveAndSlide(dp * h1.fraction, p, skinWidth, collider_size, whatIsWall, whatIsGroundEffector, 1) + MoveAndSlide(effector.Matrix.mul(dp * (1f - h1.fraction)), p + dp * h1.fraction, skinWidth, collider_size, whatIsWall, whatIsGroundEffector, 1);
                }

                // 2. 이동할 최종 위치에서 역방향으로 GroundEffector를 확인합니다.
                RaycastHit2D h2 = Physics2D.Raycast(p + dp, -dp.normalized, dp.magnitude, whatIsGroundEffector);
                if (h2.collider != null && h2.fraction != 0)
                {
                    Debug.Assert(h2.collider.TryGetComponent<GroundEffector>(out GroundEffector effector));
                    // 지면 효과와 충돌하기 전까지 이동하고, 그 이후의 움직임을 재귀 호출로 계산합니다.
                    return MoveAndSlide(dp * (1f - h2.fraction), p, skinWidth, collider_size, whatIsWall, whatIsGroundEffector, 1) + MoveAndSlide(effector.Matrix.inv_mul(dp * h2.fraction), p + dp * (1f - h2.fraction), skinWidth, collider_size, whatIsWall, whatIsGroundEffector, 1);
                }
            }

            // BoxCast를 사용하여 이동 방향으로 벽 충돌을 감지합니다.
            // p: 시작 위치, size: 박스 크기, 0f: 회전 각도, dp.normalized: 방향, l: 최대 이동 거리, whatIsWall: 벽 레이어
            RaycastHit2D h = Physics2D.BoxCast(p, size, 0f, dp.normalized, l, whatIsWall);

            // 충돌이 없으면 원래의 변위(dp)를 그대로 반환합니다.
            if (h.collider == null) return dp;

            // 충돌이 발생한 경우, 벽에 닿기 직전까지의 변위를 계산합니다.
            Vector2 move = dp.normalized * (h.distance - skinWidth);

            // 남은 변위(dp - move)를 벽을 따라 미끄러지는(slide) 움직임으로 계산합니다.
            Vector2 slide = dp - move;
            slide -= Vector2.Dot(slide, h.normal) * h.normal; // 법선 벡터 방향의 힘을 제거하여 미끄러지게 만듭니다.

            // 벽까지 이동한 거리(move)와 미끄러지는 움직임(slide)을 재귀 호출로 합산하여 최종 변위를 반환합니다.
            return move + MoveAndSlide(slide, p + move, skinWidth, collider_size, whatIsWall, whatIsGroundEffector, depth + 1);
        }
    }
}