; ModuleID = 'b8.cpp'
target datalayout = "e-m:e-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-pc-linux-gnu"

%class.b8 = type { i8 }

; Function Attrs: norecurse nounwind readonly uwtable
define zeroext i1 @_ZNK2b86is_nanEv(%class.b8* nocapture readonly %this) #0 align 2 {
  %1 = getelementptr inbounds %class.b8, %class.b8* %this, i64 0, i32 0
  %2 = load i8, i8* %1, align 1, !tbaa !1
  %3 = icmp eq i8 %2, -128
  ret i1 %3
}

; Function Attrs: norecurse nounwind uwtable
define void @_ZN2b87set_nanEv(%class.b8* nocapture %this) #1 align 2 {
  %1 = getelementptr inbounds %class.b8, %class.b8* %this, i64 0, i32 0
  store i8 -128, i8* %1, align 1, !tbaa !1
  ret void
}

; Function Attrs: norecurse nounwind readonly uwtable
define i8 @_ZNK2b8plERKS_(%class.b8* nocapture readonly %this, %class.b8* nocapture readonly dereferenceable(1) %o) #0 align 2 {
  %1 = getelementptr inbounds %class.b8, %class.b8* %this, i64 0, i32 0
  %2 = load i8, i8* %1, align 1, !tbaa !1
  %3 = getelementptr inbounds %class.b8, %class.b8* %o, i64 0, i32 0
  %4 = load i8, i8* %3, align 1, !tbaa !1
  %5 = or i8 %4, %2
  ret i8 %5
}

; Function Attrs: norecurse nounwind readonly uwtable
define i32 @_ZNK2b8orERKS_(%class.b8* nocapture readonly %this, %class.b8* nocapture readonly dereferenceable(1) %o) #0 align 2 {
  %1 = getelementptr inbounds %class.b8, %class.b8* %this, i64 0, i32 0
  %2 = load i8, i8* %1, align 1, !tbaa !1
  %3 = getelementptr inbounds %class.b8, %class.b8* %o, i64 0, i32 0
  %4 = load i8, i8* %3, align 1, !tbaa !1
  %5 = or i8 %4, %2
  %6 = zext i8 %5 to i32
  ret i32 %6
}

; Function Attrs: norecurse nounwind uwtable
define dereferenceable(1) %class.b8* @_ZN2b8ppEv(%class.b8* %this) #1 align 2 {
  %1 = getelementptr inbounds %class.b8, %class.b8* %this, i64 0, i32 0
  %2 = load i8, i8* %1, align 1, !tbaa !1
  %3 = add i8 %2, 1
  store i8 %3, i8* %1, align 1, !tbaa !1
  ret %class.b8* %this
}

; Function Attrs: norecurse nounwind uwtable
define i8 @_ZN2b8ppEi(%class.b8* nocapture %this, i32) #1 align 2 {
  %2 = getelementptr inbounds %class.b8, %class.b8* %this, i64 0, i32 0
  %3 = load i8, i8* %2, align 1, !tbaa !1
  %4 = add i8 %3, 1
  store i8 %4, i8* %2, align 1, !tbaa !1
  ret i8 %3
}

; Function Attrs: norecurse nounwind uwtable
define dereferenceable(1) %class.b8* @_ZN2b8mmEv(%class.b8* %this) #1 align 2 {
  %1 = getelementptr inbounds %class.b8, %class.b8* %this, i64 0, i32 0
  %2 = load i8, i8* %1, align 1, !tbaa !1
  %3 = add i8 %2, -1
  store i8 %3, i8* %1, align 1, !tbaa !1
  ret %class.b8* %this
}

; Function Attrs: norecurse nounwind uwtable
define i8 @_ZN2b8mmEi(%class.b8* nocapture %this, i32) #1 align 2 {
  %2 = getelementptr inbounds %class.b8, %class.b8* %this, i64 0, i32 0
  %3 = load i8, i8* %2, align 1, !tbaa !1
  %4 = add i8 %3, -1
  store i8 %4, i8* %2, align 1, !tbaa !1
  ret i8 %3
}

; Function Attrs: norecurse nounwind uwtable
define i32 @main() #1 {
  %a = alloca [2097152 x %class.b8], align 16
  %b = alloca [2097152 x %class.b8], align 16
  %c = alloca [2097152 x %class.b8], align 16
  %1 = getelementptr inbounds [2097152 x %class.b8], [2097152 x %class.b8]* %a, i64 0, i64 0, i32 0
  call void @llvm.lifetime.start(i64 2097152, i8* %1) #4
  %2 = getelementptr inbounds [2097152 x %class.b8], [2097152 x %class.b8]* %b, i64 0, i64 0, i32 0
  call void @llvm.lifetime.start(i64 2097152, i8* %2) #4
  %3 = getelementptr inbounds [2097152 x %class.b8], [2097152 x %class.b8]* %c, i64 0, i64 0, i32 0
  call void @llvm.lifetime.start(i64 2097152, i8* %3) #4
  br label %4

; <label>:4                                       ; preds = %4, %0
  %indvars.iv6 = phi i64 [ 0, %0 ], [ %indvars.iv.next7, %4 ]
  %5 = tail call i32 @rand() #4
  %6 = trunc i32 %5 to i8
  %7 = getelementptr inbounds [2097152 x %class.b8], [2097152 x %class.b8]* %a, i64 0, i64 %indvars.iv6, i32 0
  store i8 %6, i8* %7, align 1, !tbaa !1
  %8 = tail call i32 @rand() #4
  %9 = trunc i32 %8 to i8
  %10 = getelementptr inbounds [2097152 x %class.b8], [2097152 x %class.b8]* %b, i64 0, i64 %indvars.iv6, i32 0
  store i8 %9, i8* %10, align 1, !tbaa !1
  %indvars.iv.next7 = add nuw nsw i64 %indvars.iv6, 1
  %exitcond8 = icmp eq i64 %indvars.iv.next7, 2097152
  br i1 %exitcond8, label %.preheader.preheader, label %4

.preheader.preheader:                             ; preds = %4
  br label %.preheader

; <label>:11                                      ; preds = %middle.block
  %12 = getelementptr inbounds [2097152 x %class.b8], [2097152 x %class.b8]* %c, i64 0, i64 2097151, i32 0
  %13 = load i8, i8* %12, align 1, !tbaa !1
  %14 = zext i8 %13 to i32
  call void @llvm.lifetime.end(i64 2097152, i8* nonnull %3) #4
  call void @llvm.lifetime.end(i64 2097152, i8* nonnull %2) #4
  call void @llvm.lifetime.end(i64 2097152, i8* nonnull %1) #4
  ret i32 %14

.preheader:                                       ; preds = %.preheader.preheader, %middle.block
  %j.03 = phi i32 [ %37, %middle.block ], [ 0, %.preheader.preheader ]
  %15 = lshr i32 %j.03, 7
  %broadcast.splatinsert10 = insertelement <16 x i32> undef, i32 %15, i32 0
  %broadcast.splat11 = shufflevector <16 x i32> %broadcast.splatinsert10, <16 x i32> undef, <16 x i32> zeroinitializer
  br label %vector.body

vector.body:                                      ; preds = %vector.body, %.preheader
  %index = phi i64 [ 0, %.preheader ], [ %index.next.1, %vector.body ]
  %16 = getelementptr inbounds [2097152 x %class.b8], [2097152 x %class.b8]* %a, i64 0, i64 %index, i32 0
  %17 = bitcast i8* %16 to <16 x i8>*
  %wide.load = load <16 x i8>, <16 x i8>* %17, align 16, !tbaa !1
  %18 = getelementptr inbounds [2097152 x %class.b8], [2097152 x %class.b8]* %b, i64 0, i64 %index, i32 0
  %19 = bitcast i8* %18 to <16 x i8>*
  %wide.load9 = load <16 x i8>, <16 x i8>* %19, align 16, !tbaa !1
  %20 = or <16 x i8> %wide.load9, %wide.load
  %21 = zext <16 x i8> %20 to <16 x i32>
  %22 = add nuw nsw <16 x i32> %21, %broadcast.splat11
  %23 = trunc <16 x i32> %22 to <16 x i8>
  %24 = getelementptr inbounds [2097152 x %class.b8], [2097152 x %class.b8]* %c, i64 0, i64 %index, i32 0
  %25 = bitcast i8* %24 to <16 x i8>*
  store <16 x i8> %23, <16 x i8>* %25, align 16, !tbaa !1
  %index.next = or i64 %index, 16
  %26 = getelementptr inbounds [2097152 x %class.b8], [2097152 x %class.b8]* %a, i64 0, i64 %index.next, i32 0
  %27 = bitcast i8* %26 to <16 x i8>*
  %wide.load.1 = load <16 x i8>, <16 x i8>* %27, align 16, !tbaa !1
  %28 = getelementptr inbounds [2097152 x %class.b8], [2097152 x %class.b8]* %b, i64 0, i64 %index.next, i32 0
  %29 = bitcast i8* %28 to <16 x i8>*
  %wide.load9.1 = load <16 x i8>, <16 x i8>* %29, align 16, !tbaa !1
  %30 = or <16 x i8> %wide.load9.1, %wide.load.1
  %31 = zext <16 x i8> %30 to <16 x i32>
  %32 = add nuw nsw <16 x i32> %31, %broadcast.splat11
  %33 = trunc <16 x i32> %32 to <16 x i8>
  %34 = getelementptr inbounds [2097152 x %class.b8], [2097152 x %class.b8]* %c, i64 0, i64 %index.next, i32 0
  %35 = bitcast i8* %34 to <16 x i8>*
  store <16 x i8> %33, <16 x i8>* %35, align 16, !tbaa !1
  %index.next.1 = add nsw i64 %index, 32
  %36 = icmp eq i64 %index.next.1, 2097152
  br i1 %36, label %middle.block, label %vector.body, !llvm.loop !5

middle.block:                                     ; preds = %vector.body
  %37 = add nuw nsw i32 %j.03, 1
  %exitcond5 = icmp eq i32 %37, 512
  br i1 %exitcond5, label %11, label %.preheader
}

; Function Attrs: argmemonly nounwind
declare void @llvm.lifetime.start(i64, i8* nocapture) #2

; Function Attrs: nounwind
declare i32 @rand() #3

; Function Attrs: argmemonly nounwind
declare void @llvm.lifetime.end(i64, i8* nocapture) #2

attributes #0 = { norecurse nounwind readonly uwtable "disable-tail-calls"="false" "less-precise-fpmad"="false" "no-frame-pointer-elim"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+fxsr,+mmx,+sse,+sse2" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #1 = { norecurse nounwind uwtable "disable-tail-calls"="false" "less-precise-fpmad"="false" "no-frame-pointer-elim"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+fxsr,+mmx,+sse,+sse2" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #2 = { argmemonly nounwind }
attributes #3 = { nounwind "disable-tail-calls"="false" "less-precise-fpmad"="false" "no-frame-pointer-elim"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+fxsr,+mmx,+sse,+sse2" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #4 = { nounwind }

!llvm.ident = !{!0}

!0 = !{!"clang version 3.8.0-2ubuntu4 (tags/RELEASE_380/final)"}
!1 = !{!2, !3, i64 0}
!2 = !{!"_ZTS2b8", !3, i64 0}
!3 = !{!"omnipotent char", !4, i64 0}
!4 = !{!"Simple C/C++ TBAA"}
!5 = distinct !{!5, !6, !7}
!6 = !{!"llvm.loop.vectorize.width", i32 1}
!7 = !{!"llvm.loop.interleave.count", i32 1}
