using System;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Assertions.Must;


public class MovementController : MonoBehaviour
{
    public const float collisionPadding=0.015f;


    [Range(2,100)]public int numberOfHorizontalRays=4;
    [Range(2,100)]public int numberOfVerticalRay=4;

    float horizontalRaySpace;
    float verticalRaySpace;


    private BoxCollider2D coll;
    public RayCastCorners rayCastCorners;
    private PlayerMovementStats movestat;


    public bool isCollidingAbove{get ;private set;}
    public bool isCollidingBelow{get; private set;}

    public bool isCollidingLeft{get; private set;}
    public bool isCollidingRight{get;private set;}
    public int headBumpedSldieDirection{get; private set;}

    public bool isHitingCeilingCenter{get;private set;}
    public bool isHittingBothCorners{get;private set;}


    public bool isClimbingSlope{get;private set;}
    public bool wasClimbingSlopeLastFram{get; private set;}
    public bool isDecendingSlope{get;private set;}
    public float SlopeAngle{get ;private set;}
    public Vector2 slopeNormal{get;private set;}
    public float wallAngle{get;private set;}
    public bool isSliding{get;private set;}
    public bool isOnSlideableSlope{get ;private set;}
    public int faceDirection{get;private set;}
    public float ceilingAngle{get;private set;}
    public Vector2 ceilingNormal{get;private set;}

    PlayerMovement playerMovement;
    Rigidbody2D rb;


    public struct RayCastCorners
    {
        public Vector2 topLeft;
        public Vector2 topRight;
        public Vector2 bottomLeft;
        public Vector2 bottomRight;  
    }


    void Awake()
    {
        coll=GetComponent<BoxCollider2D>();
        rb=GetComponent<Rigidbody2D>();
        playerMovement=GetComponent<PlayerMovement>();
        movestat=playerMovement.moveStats;
        faceDirection=1;
    }

    void Start()
    {
        CalculateRaySpaceing();
    }

    public void Move(Vector2 velocity)
    {
        UpdateRayCastCorners();
        ResetCollisionStates();
        CheckCeilingBoxCast(velocity);
        if(velocity.y<=0f && !playerMovement.isDashing && !wasClimbingSlopeLastFram)
        {
            DescedSlope(ref velocity);
        }
        if (velocity.x != 0)
        {
            faceDirection=(int)Mathf.Sign(velocity.x);
        }

        ResolveHorizotalMovement(ref velocity);
        ResolveVerticalMovement(ref velocity);

        rb.MovePosition(rb.position + velocity);
    }

    

    private void CheckCeilingBoxCast(Vector2 velocity)
    {
        if(velocity.y<0)return;
        if(!movestat.useHeadBumpSlide)return;

        float boxCastDistance=Mathf.Abs(velocity.y)+collisionPadding;
        Vector2 boxSize=new Vector2(coll.bounds.size.x*movestat.HeadBumpBoxWidth,movestat.HeadBumpBoxHeight);
        Vector2 boxOrigin=new Vector2(coll.bounds.center.x +velocity.x,coll.bounds.max.y);

        RaycastHit2D hit=Physics2D.BoxCast(boxOrigin,boxSize,0f,Vector2.up,boxCastDistance,movestat.GroundLayer);

        if (hit)
        {
            isHitingCeilingCenter=true;

        }

        #region Debug Visualization
        if (movestat.debugShowHeadBumpBox)
        {
            Vector2 drawCenter=boxOrigin +(Vector2.up * boxCastDistance/2f);
            Vector2 drawSize=new Vector2(boxSize.x,boxSize.y +boxCastDistance);
            Vector2 halfSize=drawSize/2f;

            //4 corners of the box
            Vector2 topLeft=drawCenter +new Vector2(-halfSize.x,halfSize.y);
            Vector2 topRight=drawCenter +new Vector2(halfSize.x,halfSize.y);
            Vector2 bottomLeft=drawCenter +new Vector2(-halfSize.x,-halfSize.y);
            Vector2 bottomRight=drawCenter +new Vector2(halfSize.x,-halfSize.y); 

            Color color=hit?Color.cyan:Color.red;

            Debug.DrawLine(topLeft,topRight,color);
            Debug.DrawLine(topRight,bottomRight,color);
            Debug.DrawLine(bottomRight,bottomLeft,color);
            Debug.DrawLine(bottomLeft,topLeft,color);
        }
        #endregion
    }

    private void ResolveVerticalMovement(ref Vector2 velocity)
    {

        
        bool hitLeftCorner=false;
        bool hitRightCorner=false;
        #region CeilingCheck
        if (velocity.y >= 0f)
        {
            float upwardRayLength=Mathf.Abs(velocity.y)+collisionPadding;
            for (int i = 0; i < numberOfVerticalRay; i++)
            {
                Vector2 rayOrigin=rayCastCorners.topLeft;
                float horizontalProjection=velocity.x;
                if (playerMovement.isheadBumpSliding)
                {
                    horizontalProjection=0;
                }
                rayOrigin+=Vector2.right *(verticalRaySpace *i+horizontalProjection);
                RaycastHit2D hit =Physics2D.Raycast(rayOrigin,Vector2.up,upwardRayLength,movestat.GroundLayer);


                if (hit)
                {
                    float currentCeilingAngle;
                    if (hit.distance == 0)
                    {
                        velocity.y=0;

                        Vector2 safetyRayOrigin=rayOrigin +(Vector2.down *collisionPadding*2);
                        RaycastHit2D safetyHit=Physics2D.Raycast(safetyRayOrigin,Vector2.up,collisionPadding*3 ,movestat.GroundLayer);

                        if (safetyHit)
                        {
                            currentCeilingAngle=Mathf.Round(Vector2.Angle(safetyHit.normal,Vector2.down));
                            ceilingNormal=safetyHit.normal;
                        }
                        else
                        {
                            currentCeilingAngle=0f;
                            ceilingNormal=Vector2.down;
                        }
                    }
                    else
                    {
                        velocity.y=hit.distance-collisionPadding;
                        upwardRayLength=hit.distance;
                        currentCeilingAngle=Mathf.Round(Vector2.Angle(hit.normal,Vector2.down));
                        ceilingNormal=hit.normal;

                    }
                    isCollidingAbove=true;

                    if (i == 0)
                    {
                        hitLeftCorner=true;
                    }
                    if(i==numberOfVerticalRay-1)
                    {
                        hitRightCorner=true;

                    }
                    if (currentCeilingAngle > ceilingAngle)
                    {
                        ceilingAngle=currentCeilingAngle;
                        ceilingNormal=hit.normal;

                    }

                    if(movestat.useHeadBumpSlide && currentCeilingAngle<=movestat.maxSlopeAngleForHeadBump)
                    {
                        int slideDir=0;
                        if(i==0)slideDir=1;
                        else if(i==numberOfVerticalRay -1)slideDir=-1;

                        if(slideDir !=0)
                        {
                            Vector2 slideCheckOrigin=hit.point + (Vector2.down *collisionPadding*2);
                            float slideCheckRayLength=collisionPadding*2;
                            RaycastHit2D slideHit=Physics2D.Raycast(slideCheckOrigin,Vector2.right *slideDir,slideCheckRayLength,movestat.GroundLayer);

                            if(!slideHit)
                            {
                                headBumpedSldieDirection=slideDir;
                            }
                        }
                    }
                }

                

                #region  Debug Visualization
                if (movestat.debugShowHeadRays)
                {
                    float debugRayLength=movestat.ExtraRayDebug;
                    Vector2 debugRayOrigin=rayCastCorners.topLeft+Vector2.right*(verticalRaySpace* i +horizontalProjection);
                    bool didHit=Physics2D.Raycast(debugRayOrigin,Vector2.up,debugRayLength,movestat.GroundLayer);
                    Color rayColor=didHit?Color.cyan:Color.red;


                    if(i==0 || i == numberOfVerticalRay - 1)
                    {
                        rayColor=didHit?Color.green:Color.magenta;

                    }
                    Debug.DrawRay(debugRayOrigin,Vector2.up *debugRayLength,rayColor);
                }
                #endregion
            }
        }
        #endregion
        

        #region GroundCheck

        float downwardRayLength;
        if (velocity.y < 0)
        {
            downwardRayLength=Mathf.Abs(velocity.y)+collisionPadding;

        }
        else
        {
            downwardRayLength=collisionPadding*2;
        }
        float smallestHitDistance=float.MaxValue;
        RaycastHit2D groundHit=new RaycastHit2D();
        bool foundGround=false;


        for(int i = 0; i < numberOfVerticalRay; i++)
        {
           
            Vector2 rayOrigin =rayCastCorners.bottomLeft;
            rayOrigin +=Vector2.right *(verticalRaySpace*i +velocity.x);

            RaycastHit2D hit=Physics2D.Raycast(rayOrigin,Vector2.down,downwardRayLength,movestat.GroundLayer);

            if (hit)
            {
                if (hit.distance < smallestHitDistance)
                {
                    smallestHitDistance=hit.distance;
                    groundHit=hit;
                    foundGround=true;
                }
            }

            #region  DebugVisualization

            if (movestat.debugShowIsGrounded)
            {
                float debugRayLength=movestat.ExtraRayDebug;
                Vector2 debugRayOrigin=rayCastCorners.bottomLeft+Vector2.right *(verticalRaySpace *i +velocity.x);
                bool didHit=Physics2D.Raycast(debugRayOrigin,Vector2.down,debugRayLength,movestat.GroundLayer);
                Color rayColor=didHit?Color.cyan:Color.red;
                Debug.DrawRay(debugRayOrigin,Vector3.down *debugRayLength,rayColor);
            }


           
            #endregion
        }

        if (foundGround)
        {
            isCollidingBelow=true;
            if (velocity.y <= 0)
            {
                velocity.y=(groundHit.distance -collisionPadding)*-1;

            }
            float slopeAngle=Mathf.Round(Vector2.Angle(groundHit.normal,Vector2.up));
            bool isGroundAWall=slopeAngle >=movestat.minAngleForWallSlide && slopeAngle <=movestat.maxAngleForWallSlide;
            if (!isGroundAWall)
            {
                if (slopeAngle > 0f)
                {
                    this.SlopeAngle=slopeAngle;
                    slopeNormal=groundHit.normal;

                }
            }
        }
        else
        {
            if (isOnSlideableSlope)
            {
                isSliding=true;
                
            }
        }
        #endregion

        isHittingBothCorners=hitLeftCorner && hitRightCorner;


        if (isClimbingSlope)
        {
            float dirX=Mathf.Sign(velocity.x);
            float rayLength=Mathf.Abs(velocity.x)+collisionPadding;
            Vector2 rayOrigin=((dirX==-1)?rayCastCorners.bottomLeft:rayCastCorners.bottomRight) +Vector2.up *velocity.y;
            RaycastHit2D hit=Physics2D.Raycast(rayOrigin,Vector2.right*dirX,rayLength,movestat.GroundLayer);

            if (hit)
            {
                float slopeAngle= Mathf.Round(Vector2.Angle(hit.normal,Vector2.up));
                if (slopeAngle != SlopeAngle)
                {
                    velocity.x=(hit.distance -collisionPadding)*dirX;
                    SlopeAngle=slopeAngle;
                    slopeNormal=hit.normal;

                }
            }
        }

        #region DebugSlope Normal
        if(movestat.debugShowSlopeNormal &&(isClimbingSlope|| isDecendingSlope))
        {
            Vector2 drawOrigin=new Vector2(coll.bounds.center.x,coll.bounds.min.y);
            float drawLength=movestat.ExtraRayDebug*3f;

            Debug.DrawRay(drawOrigin,slopeNormal*drawLength,Color.yellow);
        }
        #endregion

    }

    private void ResolveHorizotalMovement(ref Vector2 velocity)
    {
        float dirX=Mathf.Sign(velocity.x);
        if (velocity.x == 0f)
        {
            dirX=faceDirection;
        }
        float rayLength=Mathf.Abs(velocity.x) + collisionPadding;

        if (Mathf.Abs(velocity.x) < collisionPadding)
        {
            rayLength =collisionPadding *2;

        }
        for(int i = 0; i < numberOfHorizontalRays; i++)
        {
            Vector2 rayOrigin=(dirX==-1)?rayCastCorners.bottomLeft:rayCastCorners.bottomRight;
            rayOrigin +=Vector2.up *(horizontalRaySpace*i);
            RaycastHit2D hit=Physics2D.Raycast(rayOrigin,Vector2.right *dirX ,rayLength,movestat.GroundLayer);


            if (hit)
            {
                float slopeAngle =Mathf.Round(Vector2.Angle(hit.normal,Vector2.up));
                bool isSliediableslope=slopeAngle>movestat.maxSlopeAngle && slopeAngle<movestat.minAngleForWallSlide;
                if (isSliediableslope)
                {
                    isOnSlideableSlope=true;
                    velocity.x=(hit.distance -collisionPadding)*dirX;
                    rayLength=hit.distance;
                    if (isClimbingSlope)
                    {
                        velocity.y=Mathf.Tan(slopeAngle*Mathf.Deg2Rad)*Mathf.Abs(velocity.x);

                    }

                    if(dirX == -1)
                    {
                        isCollidingLeft=true;

                    }else if(dirX== 1)
                    {
                        isCollidingRight=true;
                    }

                    continue;

                }

                if (isSliding)
                {
                    continue;
                }

                if(i==0 && slopeAngle <= movestat.maxSlopeAngle)
                {
                    ClimbSlope(ref velocity,slopeAngle,hit.normal);
                    continue;
                }
                if(isClimbingSlope && slopeAngle <= movestat.maxSlopeAngle)
                {
                    continue;
                }

                velocity.x=(hit.distance -collisionPadding)*dirX;
                rayLength=hit.distance;
                wallAngle=slopeAngle;


                if (isClimbingSlope)
                {
                    velocity.y=Mathf.Tan(SlopeAngle*Mathf.Deg2Rad)* Mathf.Abs(velocity.x);

                }

                isCollidingLeft=dirX==-1;
                isCollidingRight=dirX==1;


            }

            #region  Debug Visualization


            if (movestat.debugShowWallHit)
            {
                float debugRayLength=movestat.ExtraRayDebug;
                Vector2 debugRayOrigin=(dirX ==-1)?rayCastCorners.bottomLeft:rayCastCorners.bottomRight;
                debugRayOrigin +=Vector2.up * (horizontalRaySpace*i);

                bool didHit=Physics2D.Raycast(debugRayOrigin,Vector2.right *dirX ,debugRayLength,movestat.GroundLayer);
                Color rayColor=didHit?Color.cyan:Color.red;
                Debug.DrawRay(debugRayOrigin, Vector2.right* dirX *debugRayLength,rayColor);
            }
            #endregion
        }
    }

    
    private void ResetCollisionStates()
    {
        isCollidingAbove=false;
        isCollidingBelow=false;
        isCollidingLeft=false;
        isCollidingRight=false;

        isHitingCeilingCenter=false;
        isHittingBothCorners=false;

        headBumpedSldieDirection=0;
        wasClimbingSlopeLastFram=isClimbingSlope;
        isDecendingSlope=false;
        isClimbingSlope=false;
        SlopeAngle=0f;
        slopeNormal=Vector2.zero;
        wallAngle=0f;
        isSliding=false;
        isOnSlideableSlope=false;
        ceilingAngle=0f;
        ceilingNormal=Vector2.zero;
    }

    private void UpdateRayCastCorners()
    {
        Bounds bounds= coll.bounds;
        bounds.Expand(collisionPadding *-2);

        rayCastCorners.bottomLeft=new Vector2(bounds.min.x,bounds.min.y);
        rayCastCorners.bottomRight=new Vector2(bounds.max.x,bounds.min.y);
        rayCastCorners.topLeft=new Vector2(bounds.min.x,bounds.max.y);
        rayCastCorners.topRight=new Vector2(bounds.max.x,bounds.max.y);
    }
    private void CalculateRaySpaceing()
    {
        Bounds bounds=coll.bounds;
        bounds.Expand(collisionPadding *-2);

        horizontalRaySpace =bounds.size.y/(numberOfHorizontalRays-1);
        verticalRaySpace =bounds.size.x/(numberOfVerticalRay-1);
    }



    #region Helper Methods


    public bool IsGrounded()=>isCollidingBelow;

    public bool BumpedHead()=>isCollidingAbove;
    public bool IsTouchingWall(bool isFacingRight)=>(isFacingRight &&isCollidingRight)|| (!isFacingRight && isCollidingLeft);

    public int GetWallDirection()
    {
        if(isCollidingLeft)return -1;
        if(isCollidingRight)return 1;
        return 0;
    }
    #endregion

    #region Slopes
    private void ClimbSlope(ref Vector2 velocity, float slopeAngle, Vector2 normal)
    {
        float moveDistance=Mathf.Abs(velocity.x);
        float climbVelocityY=Mathf.Sin(slopeAngle *Mathf.Deg2Rad)*moveDistance;


        if(velocity.y <= climbVelocityY)
        {
            velocity.y=climbVelocityY;
            velocity.x=Mathf.Cos(slopeAngle *Mathf.Deg2Rad)*moveDistance*Mathf.Sign(velocity.x);

            isCollidingBelow=true;
            isClimbingSlope=true;
            SlopeAngle=slopeAngle;
            slopeNormal=normal;
        }
    }
    private void DescedSlope(ref Vector2 velocity)
    {
        RaycastHit2D maxSlopeHitLeft=Physics2D.Raycast(rayCastCorners.bottomLeft,Vector2.down,Mathf.Abs(velocity.y)+collisionPadding ,movestat.GroundLayer);
        RaycastHit2D maxSlopeHitRight=Physics2D.Raycast(rayCastCorners.bottomRight,Vector2.down,Mathf.Abs(velocity.y)+collisionPadding ,movestat.GroundLayer);

        if (maxSlopeHitLeft ^ maxSlopeHitRight)
        {
            SlideDownMaxSlope(maxSlopeHitLeft,ref velocity);
            SlideDownMaxSlope(maxSlopeHitRight,ref velocity);

        }

        if (!isSliding)
        {
            float dirX=faceDirection;
            Vector2 rayOrigin=(dirX==-1)?rayCastCorners.bottomRight:rayCastCorners.bottomLeft;

            float maxExpectationVerticalDrop=Mathf.Tan(movestat.maxSlopeAngle *Mathf.Deg2Rad)*Mathf.Abs(velocity.x);
            float dynamicRayLength=Mathf.Abs(velocity.y)+collisionPadding +maxExpectationVerticalDrop;
            float rayLength=Mathf.Max(dynamicRayLength,collisionPadding *2f);

            RaycastHit2D hit=Physics2D.Raycast(rayOrigin,Vector2.down,rayLength,movestat.GroundLayer);

            if (hit)
            {
                float slopeAngle=Mathf.Round(Vector2.Angle(hit.normal,Vector2.up));
                ApplySlopeStick(ref velocity,slopeAngle,hit);

            }

        }
    }

    
    private void ApplySlopeStick(ref Vector2 moveAmount, float slopeAngle, RaycastHit2D hit)
    {
        if(hit.distance-collisionPadding <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x))
        {
            float moveDistance=Mathf.Abs(moveAmount.x);
            float desecedMoveAmountY =Mathf.Sin(slopeAngle*Mathf.Deg2Rad)*moveDistance;

            moveAmount.x=Mathf.Cos(slopeAngle *Mathf.Deg2Rad)*moveDistance*Mathf.Sign(moveAmount.x);
            moveAmount.y-=desecedMoveAmountY;

            isDecendingSlope=true;
            isCollidingBelow=true;
            slopeNormal=hit.normal;
            SlopeAngle=slopeAngle;
            
        }
    }

    private void SlideDownMaxSlope(RaycastHit2D hit, ref Vector2 velocity)
    {
        if (hit)
        {
            float slopeAngle=Mathf.Round(Vector2.Angle(hit.normal,Vector2.up));
            int wallDirection=(int)Mathf.Sign(hit.normal.x);
            bool isFacingWall=(wallDirection ==-1 && playerMovement.isFacingRight)||(wallDirection==1 &&!playerMovement.isFacingRight);

            bool isNormalSliedableSlope=slopeAngle > movestat.maxSlopeAngle && slopeAngle<movestat.maxAngleForWallSlide;
            bool isWallSlope=slopeAngle>=movestat.minAngleForWallSlide;

            if(isNormalSliedableSlope || (isWallSlope && !isFacingWall))
            {
                float tanAngle=Mathf.Clamp(slopeAngle,0,89.9f);

                velocity.x=Mathf.Sign(hit.normal.x)*(Mathf.Abs(velocity.y) -hit.distance)/Mathf.Tan(tanAngle *Mathf.Deg2Rad);

                isSliding=true;
                isCollidingBelow=true;
                SlopeAngle=slopeAngle;
                slopeNormal=hit.normal;
            }
        }
    }




    #endregion
}
