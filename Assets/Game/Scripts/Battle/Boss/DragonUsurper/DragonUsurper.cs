using MonsterAction.Combat;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

namespace MonsterAction.Battle.Boss
{    
    public class DragonUsurper : BossBase
    {
        [Header("距離")]
        [SerializeField] private float meleeRange = 7f;
        [SerializeField] private float farRange = 18f;

        [Header("移動")]
        [SerializeField] private float walkSpeed = 20f;

        [Header("攻撃判定（ボーンの子に置いた BossAttackHitbox）")]
        [SerializeField] private BossAttackHitbox biteHitbox;     // 口（ブレスの発射位置にも使う）
        [SerializeField] private BossAttackHitbox bodyHitbox;     // 胴（ジャンプ攻撃用）

        [Header("小攻撃")]
        [SerializeField] private float biteDamage = 20f;

        [Header("ブレス★（正面へ扇状に、地面すれすれをまっすぐ飛ぶ）")]
        [SerializeField] private BossProjectile breathPrefab; // 空なら球を自動で作る
        [SerializeField] private float breathDamage = 35f;
        [SerializeField] private float breathSpeed = 20f;
        [SerializeField] private float breathRadius = 1.0f;
        [Tooltip("一度に出す火球の数")]
        [SerializeField] private int breathCount = 3;
        [Tooltip("火球どうしの角度（度）")]
        [SerializeField] private float breathSpreadAngle = 12f;
        [Tooltip("飛ぶ高さ（m）。プレイヤーの胸あたり")]
        [SerializeField] private float breathHeight = 1.5f;
        [SerializeField] private float flyBreathHeight = 7f;
        [Tooltip("ボスの中心からどれだけ前で出すか（m）")]
        [SerializeField] private float breathStartForward = 4f;
        [SerializeField] private Color breathColor = new Color(1f, 0.45f, 0.1f);

        [Header("咆哮")]
        [SerializeField] private float roarRadius = 12f;
        [SerializeField] private float roarDamage = 0f;
        [SerializeField] private GameObject roarEffect;

        [Header("ジャンプ攻撃")]
        [SerializeField] private float jumpAttackRadius = 7f;
        [SerializeField] private float jumpDamage = 15f;

        [Header("歩き時間")]
        [SerializeField] private float walkTime = 5f;

        [Header("怒り")]
        [Tooltip("怒り状態のモーション速度")]
        [SerializeField] private float enragedAnimSpeed = 1.2f;
        [Tooltip("怒り状態の休み時間の倍率")]
        [SerializeField] private float enragedRestRate = 0.6f;

        // Animator のステート名（DragonUsorper 用の Animator Controller と同じ名前）
        private const string Walk = "Walk";
        private const string Run = "Run";
        private const string BasicAttack = "Basic Attack";
        private const string Scream = "Scream";
        private const string FlameAttack = "Flame Attack";
        private const string ClawAttack = "Claw Attack";
        private const string TakeOff = "Take Off";
        private const string FlyFlameAttack = "Fly Flame Attack";
        private const string Land = "Land";

        private bool hasRoared;
        private bool enraged;
        private float RestTime(float seconds) => enraged ? seconds * enragedRestRate : seconds;//bool型変数の後に？をその後の処理に:を付けることでifを省略して条件分岐させることができる

        // ================================================================
        //  どの行動をするか決める
        // ================================================================

        /// <summary>次の行動を決めて返す（行動が終わるたびに呼ばれる）</summary>
        protected override IEnumerator ThinkNextAction()
        {
            if (!hasRoared) { hasRoared = true; return Roar(); }//hasRoaredがfalseならRoarを実行する

            if (!enraged && HpRate <= 0.5f) { enraged = true; Anim.speed = enragedAnimSpeed;return Roar(); }//HpRateで体力の割合を管理、5割を下回ったら怒り状態をONにしてRoar
            
            float dis = DistanceToPlayer;
            int r = Random.Range(0, 100);
            if (dis <= meleeRange) //近距離の処理
            {
                if (r < 60) return Bite();//0～59で噛みつき(60%)
                else if (r >= 60 && r < 80) return JumpAttack();//60～79でジャンプ攻撃(20%)
                else if (r >= 80 && r < 100) return Breath();//80～99でブレス(20%)
            }

            if (dis <= farRange) //中距離の処理
            {
                if (r < 50) return Breath();//0～49でブレス(50%)
                else if (r >= 50 && r < 80) return FlyBreath();//50～79で飛行ブレス(30%)
                else if (r >= 80 && r < 100) return Approach(walkTime);//80～99で移動(20%)
            }

            if (dis > farRange) //遠距離の処理
            {
                if (r < 45) return Breath();//0～44でブレス(45%)
                else if (r >= 45 && r < 90) return FlyBreath();//45～89で飛行ブレス(45%)
                else if (r >= 90 && r < 100) return Approach(walkTime);//90～99で移動(10%)
            }
            
            //4番
            return null;
        }

        // ================================================================
        //  咆哮
        // ================================================================

        /// <summary>咆哮：プレイヤーの方を向いて吠える。近くにいるとのけぞらせる</summary>
        private IEnumerator Roar()
        {
            
            yield return TurnToPlayer(1f, Walk);//Playerに1秒向き直す、アニメーションはWalkを参照

            Play(Scream);//Screamモーションを再生する。遷移時間は初期値の0.15f
            
            yield return WaitStateTime(Scream, 0.25f);//Screamの25%まで進めたらWaitStateTimeを返す。GetStateTimeは(stateName)がどこまで再生されたか確認する。

            AreaAttack(transform.position, roarRadius, roarDamage, knockback: 1.5f);//AreaAttackメソッド、現在地点からroarRadius半径分、roarﾀﾞﾒｰｼﾞで1.5mノックバックする

            if (roarEffect != null)
            {
                Instantiate(roarEffect, transform.position, Quaternion.Euler(0f, 0f, 0f));
            }

            yield return WaitStateTime(Scream, 0.95f);//Screamの95%まで進めたらWaitStateTimeを返す
            
            yield return Rest(RestTime(0.5f));//RestTime0.5秒
        }

        // ================================================================
        //  小攻撃
        // ================================================================

        /// <summary>噛みつき（小）：軸合わせしながら踏み込んで噛む</summary>
        private IEnumerator Bite()
        {
            yield return TurnToPlayer(0.5f, Walk);
            
            Play(BasicAttack);
            
            while (GetStateTime(BasicAttack) < 0.3f) 
            {
                FaceTowards(DirectionToPlayer, 1.5f);
                Move(transform.forward * 2.5f);
                yield return null;
            }
            
            biteHitbox.Begin(biteDamage, knockback: 1f);//口の攻撃判定

            yield return WaitStateTime(BasicAttack, 0.65f);
            biteHitbox.End();//  噛みつき65%までで判定終了

            yield return WaitStateTime(BasicAttack, 0.95f);
            yield return Rest(RestTime(0.5f));

        }




        // ================================================================
        //  遠距離攻撃（大）
        // ================================================================

        /// <summary>
        /// ブレス★：軸合わせしてから、正面へ扇状に火球を撃つ。
        /// 狙いはプレイヤーの位置ではなく「ボスの正面」。地面すれすれをまっすぐ飛ぶので、正面にいればほぼ当たる
        /// </summary>
        private IEnumerator Breath()
        {
            
            Play(FlameAttack);
            yield return WaitStateTimeTracking(FlameAttack, 0.35f, 1.2f);

            Vector3 start = transform.position + transform.forward * breathStartForward + Vector3.up * breathHeight;//スタート位置は位置+(角度掛けるブレス開始位置)+Y軸掛けるブレスの飛ぶ高さ
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            // プレイヤーに向かう方向を取得
            Vector3 targetPosition = player.transform.position + Vector3.up * 2.0f;

            Vector3 targetDir = (targetPosition - start).normalized;
            foreach (Vector3 dir in FanDirections(targetDir, breathCount, breathSpreadAngle))
            {
                var ball = BossProjectile.Spawn(breathPrefab, start, breathRadius, breathColor);

                ball.Launch(dir * breathSpeed,
                    MakeDamage(breathDamage, knockback: 4, knockDown: true),
                    transform);
            }

            yield return WaitStateTimeTracking(FlameAttack, 0.45f, 1.2f);
            foreach (Vector3 dir in FanDirections(targetDir, breathCount, breathSpreadAngle))
            {
                var ball = BossProjectile.Spawn(breathPrefab, start, breathRadius, breathColor);

                ball.Launch(dir * breathSpeed,
                    MakeDamage(breathDamage, knockback: 4, knockDown: true),
                    transform);
            }

            yield return WaitStateTimeTracking(FlameAttack, 0.55f, 1.2f);
            foreach (Vector3 dir in FanDirections(targetDir, breathCount, breathSpreadAngle))
            {
                var ball = BossProjectile.Spawn(breathPrefab, start, breathRadius, breathColor);

                ball.Launch(dir * breathSpeed,
                    MakeDamage(breathDamage, knockback: 4, knockDown: true),
                    transform);
            }

            yield return WaitStateTime(FlameAttack, 0.95f);
            yield return Rest(RestTime(0.8f));
        }

        private IEnumerator FlyBreath()
        {
            Play(TakeOff);
            yield return WaitStateTimeTracking(TakeOff, 0.95f, 1.2f);
            yield return WaitStateTimeTracking(FlyFlameAttack, 0.35f, 1.2f);

            Vector3 start = transform.position + transform.forward * breathStartForward + Vector3.up * flyBreathHeight;
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            // プレイヤーに向かう方向を取得
            Vector3 targetPosition = player.transform.position + Vector3.up * 1.0f;

            Vector3 targetDir = (targetPosition - start).normalized;

            foreach (Vector3 dir in FanDirections(targetDir, breathCount, breathSpreadAngle))
            {
                var ball = BossProjectile.Spawn(breathPrefab, start, breathRadius, breathColor);

                ball.Launch(dir * breathSpeed,
                    MakeDamage(breathDamage, knockback: 4, knockDown: true),
                    transform);
            }

            yield return WaitStateTimeTracking(FlyFlameAttack, 0.45f, 1.2f);
            foreach (Vector3 dir in FanDirections(targetDir, breathCount, breathSpreadAngle))
            {
                var ball = BossProjectile.Spawn(breathPrefab, start, breathRadius, breathColor);

                ball.Launch(dir * breathSpeed,
                    MakeDamage(breathDamage, knockback: 4, knockDown: true),
                    transform);
            }

            yield return WaitStateTimeTracking(FlyFlameAttack, 0.55f, 1.2f);
            foreach (Vector3 dir in FanDirections(targetDir, breathCount, breathSpreadAngle))
            {
                var ball = BossProjectile.Spawn(breathPrefab, start, breathRadius, breathColor);

                ball.Launch(dir * breathSpeed,
                    MakeDamage(breathDamage, knockback: 4, knockDown: true),
                    transform);
            }

            yield return WaitStateTime(FlyFlameAttack, 0.95f);
            yield return WaitStateTime(Land, 0.95f);
            yield return Rest(RestTime(0.8f));
        }


        /// <summary>正面を中心に、count 本の方向を angle 度ずつ左右に広げて返す（水平）</summary>
        private IEnumerable<Vector3> FanDirections(Vector3 forward, int count, float spreadAngle)
        {
            forward.Normalize();

            if (count <= 1)
            {
                yield return forward;
                yield break;
            }

            for (int i = 0; i < count; i++)
            {
                float angle = -spreadAngle + (spreadAngle * 2f / (count - 1)) * i;

                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * forward;

                yield return dir;
            }
        }

        // ================================================================
        //  ジャンプ攻撃（大）
        // ================================================================
        private IEnumerator JumpAttack()
        {
            Play(ClawAttack);

            while (GetStateTime(ClawAttack) < 0.3f)
            {
                FaceTowards(DirectionToPlayer, 1.5f);
                Move(transform.forward * 2.5f);
                yield return null;
            }

            bodyHitbox.Begin(jumpDamage, knockback: 1.5f, knockDown:true);//体の攻撃判定

            yield return WaitStateTime(ClawAttack, 0.65f);
            bodyHitbox.End();//  ジャンプ攻撃65%までで判定終了

            yield return WaitStateTime(ClawAttack, 0.95f);
            yield return Rest(RestTime(0.8f));

        }

        //追加部分終了

        // ================================================================
        //  移動
        // ================================================================

        /// <summary>走って近づく</summary>
        private IEnumerator Approach(float maxSeconds)
        {
            Play(Run, 0.25f);

            float end = Time.time + maxSeconds;

            while (Time.time < end && DistanceToPlayer > meleeRange * 0.8f)
            {
                FaceTowards(DirectionToPlayer);
                Move(transform.forward * walkSpeed);
                yield return null;
            }
            yield return null;
        }

       
        //ギズモ表示用
        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, meleeRange);
            Gizmos.DrawWireSphere(transform.position, farRange);
        }

#if UNITY_EDITOR
        /// <summary>セットアップツール用（消さないこと）</summary>
        public void EditorSetup(BossAttackHitbox bite, BossAttackHitbox body)
        {
            biteHitbox = bite;
            bodyHitbox = body;
        }
#endif
    }
}
