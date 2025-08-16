using System.Collections;
using UnityEngine;

namespace BoxCC
{
    // [RequireComponent]는 이 스크립트가 추가된 게임 오브젝트에
    // Rigidbody2D, BoxCollider2D, Animator 컴포넌트가 자동으로 추가되도록 합니다.
    // 이는 캐릭터 컨트롤러의 필수 컴포넌트들입니다.
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Animator))]
    public class CharacterBase : MonoBehaviour
    {
        // 컴포넌트 변수
        // 캐릭터에 필요한 필수 컴포넌트들을 선언합니다.
        private Rigidbody2D m_Rigidbody; // 물리 연산을 위한 Rigidbody2D 컴포넌트
        private BoxCollider2D m_Collider; // 충돌 감지를 위한 BoxCollider2D 컴포넌트

        // 이동 관련 변수
        // 캐릭터의 이동 속도, 방향 등 이동 로직에 사용되는 변수들입니다.
        protected Vector2 movementIS; // 키보드 입력에 따른 이동 방향 (Input Space)
        private Vector2 _movementWS; // 실제 월드 좌표계에서의 이동 방향 (World Space)
        protected float isRun = 0; // 달리기를 위한 값 (0: 걷기, 1: 달리기)

        // 설정 값 (Inspector 노출 변수)
        // 인스펙터에서 설정할 수 있는 변수들로, 게임플레이에 영향을 줍니다.
        [Header("Physics")]
        [SerializeField] private Vector2 _moveSpeedMinMax = new Vector2(1.5f, 3f); // 최소(걷기)/최대(달리기) 이동 속도
        [SerializeField] private LayerMask _wallLayerMask = 0; // 벽으로 간주할 레이어 마스크
        [SerializeField] private LayerMask _groundEffectorLayerMask = 0; // 지면 효과(경사로 등) 레이어 마스크
        [SerializeField] private Vector4 _groundEffectorMatrix = new Vector4(1, 0, 0, 1); // 지면 효과를 적용하는 데 사용되는 2x2 행렬
        [SerializeField] private Vector2 _depthMinMax; // 캐릭터의 깊이(z축) 최소/최대값

        // 내부 사용 변수
        // 로직 처리를 위해 내부적으로만 사용되는 변수들입니다.
        private Coroutine m_OnLateFixedUpdate; // LateFixedUpdate 코루틴을 제어하기 위한 변수
        private WaitForFixedUpdate _waitForFixedUpdate; // FixedUpdate가 끝날 때까지 기다리는 데 사용되는 객체
        private float _skinWidth = 0.01f; // 충돌 감지를 위한 스킨 폭. 좁은 틈에 끼이는 것을 방지합니다.

        // MonoBehaviour가 활성화될 때 한 번 호출되는 메서드입니다.
        // 모든 컴포넌트가 생성된 후 변수를 할당하는 데 사용됩니다.
        public void Awake()
        {
            // 컴포넌트 변수 할당
            m_Rigidbody = GetComponent<Rigidbody2D>();
            m_Collider = GetComponent<BoxCollider2D>();
        }

        // 스크립트가 활성화될 때마다 호출됩니다.
        void OnEnable()
        {
            // FixedUpdate 대기 객체를 새로 생성합니다.
            _waitForFixedUpdate = new();
            // LateFixedUpdate 코루틴을 시작하고 참조를 저장합니다.
            m_OnLateFixedUpdate = StartCoroutine(OnLateFixedUpdate());
        }

        // 스크립트가 비활성화될 때 호출됩니다.
        void OnDisable()
        {
            // 코루틴과 대기 객체 참조를 초기화합니다.
            // **주의: 코루틴을 명시적으로 중단하는 코드가 필요할 수 있습니다.**
            if (m_OnLateFixedUpdate != null)
            {
                StopCoroutine(m_OnLateFixedUpdate);
            }
            m_OnLateFixedUpdate = null;
            _waitForFixedUpdate = null;
        }

        // 고정된 시간 간격으로 호출되는 물리 업데이트 함수입니다.
        void FixedUpdate()
        {
            // 걷기(isRun=0)와 달리기(isRun=1) 속도를 보간하여 현재 이동 속도를 계산합니다.
            // movementIS는 입력 벡터, MathExtensions.select는 속도 보간 함수입니다.
            Vector2 _movementOS = movementIS * MathExtensions.select(_moveSpeedMinMax, isRun);

            // GroundEffector 행렬을 사용하여 이동 벡터를 월드 좌표계로 변환합니다.
            _movementWS = _groundEffectorMatrix.mul(_movementOS);

            // 현재 위치에 GroundEffector가 있는지 감지합니다.
            Collider2D groundEffectorTrigger =
                Physics2D.OverlapPoint(m_Rigidbody.position,
                                        _groundEffectorLayerMask,
                                        _depthMinMax.x,
                                        _depthMinMax.y);
            
            // GroundEffector를 감지했을 경우, 해당 지면의 행렬을 가져와 적용합니다.
            if (groundEffectorTrigger != null && groundEffectorTrigger.isTrigger)
            {
                Debug.Assert(groundEffectorTrigger.TryGetComponent<GroundEffector>(out GroundEffector effector));
                _groundEffectorMatrix = effector.Matrix;
            }
            // GroundEffector가 없으면, 기본값(단위 행렬)으로 초기화합니다.
            else
            {
                _groundEffectorMatrix = new Vector4(1, 0, 0, 1);
            }
        }

        /// <summary>
        /// FixedUpdate 이후에 실행되는 함수입니다.
        /// 물리 연산이 완료된 후 캐릭터를 움직이는 데 사용됩니다.
        /// </summary>
        IEnumerator OnLateFixedUpdate()
        {
            // 게임이 실행되는 동안 반복합니다.
            while (Application.isPlaying)
            {
                float dt = Time.fixedDeltaTime;

                // FixedUpdate가 끝날 때까지 기다립니다.
                yield return _waitForFixedUpdate;

                // PhyscisSolver를 사용하여 충돌을 고려한 최종 움직임을 계산합니다.
                Vector2 dp = PhyscisSolver.MoveAndSlide(
                    _movementWS * dt,
                    m_Rigidbody.position,
                    _skinWidth,
                    m_Collider.size,
                    _wallLayerMask,
                    _groundEffectorLayerMask);

                // 계산된 움직임(dp)만큼 캐릭터의 위치를 이동시킵니다.
                m_Rigidbody.MovePosition(m_Rigidbody.position + dp);
            }

            yield break;
        }
    }
}