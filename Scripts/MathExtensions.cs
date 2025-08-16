using UnityEngine;
namespace BoxCC
{
    // MathExtensions 클래스는 C#의 확장(extension) 메서드를 사용하여
    // Vector와 같은 Unity 내장 타입의 기능을 확장합니다.
    public static class MathExtensions
    {
        // 이 메서드는 2x2 행렬과 2차원 벡터의 곱셈을 수행합니다.
        // Vector4는 (x, y, z, w)를 (a, b, c, d)로 간주하여 행렬로 사용합니다.
        // 행렬 a = | a.x  a.z |
        //         | a.y  a.w |
        // 벡터 b = | b.x |
        //         | b.y |
        // 결과는 (a.x*b.x + a.z*b.y, a.y*b.x + a.w*b.y)가 됩니다.
        public static Vector2 mul(this Vector4 a, Vector2 b)
        {
            return new Vector2(a.x * b.x + a.z * b.y, a.y * b.x + a.w * b.y);
        }

        // 이 메서드는 행렬과 벡터의 곱셈에서, 곱셈 전의 벡터(b)를 역으로 계산합니다.
        // 즉, a * b = c 일 때 b를 구하는 역할을 합니다.
        // b = (a의 역행렬) * c
        public static Vector2 inv_mul(this Vector4 a, Vector2 c)
        {
            // 먼저 2x2 행렬의 행렬식을 계산합니다.
            // 행렬식(Determinant)은 ad - bc 입니다. 여기서는 a.x * a.w - a.z * a.y
            float det = a.x * a.w - a.z * a.y;

            // 행렬식이 0에 가까우면 역행렬이 존재하지 않습니다.
            // 이 경우, 오류를 출력하고 계산을 중단합니다.
            if (Mathf.Approximately(det, 0f))
            {
                Debug.LogError("The matrix is singular. Cannot calculate the inverse.");
                return Vector2.zero;
            }

            // 역행렬을 계산하기 위해 행렬식의 역수(1/det)를 구합니다.
            float invDet = 1.0f / det;

            // 역행렬 공식을 사용하여 원래 벡터 b의 x와 y 값을 계산합니다.
            // 역행렬 공식: 1/det * | a.w  -a.z |
            //                      | -a.y  a.x |
            float bx = invDet * (a.w * c.x - a.z * c.y);
            float by = invDet * (-a.y * c.x + a.x * c.y);

            return new Vector2(bx, by);
        }

        // 이 메서드는 두 값(minMax) 사이를 보간(Interpolate)하여 하나의 값을 반환합니다.
        // b가 0이면 minMax.x를, b가 1이면 minMax.y를 반환합니다.
        // b가 0과 1 사이의 값일 경우, 두 값 사이의 중간 값을 반환합니다.
        // 선형 보간(Lerp)과 동일한 원리입니다.
        // 공식: (시작값 * (1 - 비율)) + (끝값 * 비율)
        public static float select(Vector2 minMax, float b)
        {
            return minMax.x * (1f - b) + minMax.y * b;
        }
    }
}