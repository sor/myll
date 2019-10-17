; ModuleID = 'b8.cpp'
target datalayout = "e-m:e-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-pc-linux-gnu"

%"class.std::ios_base::Init" = type { i8 }
%"class.std::basic_ostream" = type { i32 (...)**, %"class.std::basic_ios" }
%"class.std::basic_ios" = type { %"class.std::ios_base", %"class.std::basic_ostream"*, i8, i8, %"class.std::basic_streambuf"*, %"class.std::ctype"*, %"class.std::num_put"*, %"class.std::num_get"* }
%"class.std::ios_base" = type { i32 (...)**, i64, i64, i32, i32, i32, %"struct.std::ios_base::_Callback_list"*, %"struct.std::ios_base::_Words", [8 x %"struct.std::ios_base::_Words"], i32, %"struct.std::ios_base::_Words"*, %"class.std::locale" }
%"struct.std::ios_base::_Callback_list" = type { %"struct.std::ios_base::_Callback_list"*, void (i32, %"class.std::ios_base"*, i32)*, i32, i32 }
%"struct.std::ios_base::_Words" = type { i8*, i64 }
%"class.std::locale" = type { %"class.std::locale::_Impl"* }
%"class.std::locale::_Impl" = type { i32, %"class.std::locale::facet"**, i64, %"class.std::locale::facet"**, i8** }
%"class.std::locale::facet" = type <{ i32 (...)**, i32, [4 x i8] }>
%"class.std::basic_streambuf" = type { i32 (...)**, i8*, i8*, i8*, i8*, i8*, i8*, %"class.std::locale" }
%"class.std::ctype" = type <{ %"class.std::locale::facet.base", [4 x i8], %struct.__locale_struct*, i8, [7 x i8], i32*, i32*, i16*, i8, [256 x i8], [256 x i8], i8, [6 x i8] }>
%"class.std::locale::facet.base" = type <{ i32 (...)**, i32 }>
%struct.__locale_struct = type { [13 x %struct.__locale_data*], i16*, i32*, i32*, [13 x i8*] }
%struct.__locale_data = type opaque
%"class.std::num_put" = type { %"class.std::locale::facet.base", [4 x i8] }
%"class.std::num_get" = type { %"class.std::locale::facet.base", [4 x i8] }
%class.b8 = type { i8 }

@_ZStL8__ioinit = internal global %"class.std::ios_base::Init" zeroinitializer, align 1
@__dso_handle = external global i8
@_ZSt4cout = external global %"class.std::basic_ostream", align 8
@llvm.global_ctors = appending global [1 x { i32, void ()*, i8* }] [{ i32, void ()*, i8* } { i32 65535, void ()* @_GLOBAL__sub_I_b8.cpp, i8* null }]

declare void @_ZNSt8ios_base4InitC1Ev(%"class.std::ios_base::Init"*) #0

; Function Attrs: nounwind
declare void @_ZNSt8ios_base4InitD1Ev(%"class.std::ios_base::Init"*) #1

; Function Attrs: nounwind
declare i32 @__cxa_atexit(void (i8*)*, i8*, i8*) #2

; Function Attrs: norecurse nounwind readonly uwtable
define zeroext i1 @_ZNK2b86is_nanEv(%class.b8* nocapture readonly %this) #3 align 2 {
  %1 = getelementptr inbounds %class.b8, %class.b8* %this, i64 0, i32 0
  %2 = load i8, i8* %1, align 1, !tbaa !1
  %3 = icmp eq i8 %2, -1
  ret i1 %3
}

; Function Attrs: norecurse nounwind uwtable
define void @_ZN2b87set_nanEv(%class.b8* nocapture %this) #4 align 2 {
  %1 = getelementptr inbounds %class.b8, %class.b8* %this, i64 0, i32 0
  store i8 -1, i8* %1, align 1, !tbaa !1
  ret void
}

; Function Attrs: norecurse nounwind readonly uwtable
define zeroext i1 @_ZNK2b8ntEv(%class.b8* nocapture readonly %this) #3 align 2 {
  %1 = getelementptr inbounds %class.b8, %class.b8* %this, i64 0, i32 0
  %2 = load i8, i8* %1, align 1, !tbaa !1
  %3 = icmp ne i8 %2, 0
  ret i1 %3
}

; Function Attrs: norecurse nounwind readonly uwtable
define i8 @_ZNK2b8coEv(%class.b8* nocapture readonly %this) #3 align 2 {
  %1 = getelementptr inbounds %class.b8, %class.b8* %this, i64 0, i32 0
  %2 = load i8, i8* %1, align 1, !tbaa !1
  %3 = xor i8 %2, -1
  ret i8 %3
}

; Function Attrs: norecurse nounwind readonly uwtable
define i8 @_ZNK2b8plERKS_(%class.b8* nocapture readonly %this, %class.b8* nocapture readonly dereferenceable(1) %o) #3 align 2 {
  %1 = getelementptr inbounds %class.b8, %class.b8* %this, i64 0, i32 0
  %2 = load i8, i8* %1, align 1, !tbaa !1
  %3 = getelementptr inbounds %class.b8, %class.b8* %o, i64 0, i32 0
  %4 = load i8, i8* %3, align 1, !tbaa !1
  %5 = or i8 %4, %2
  ret i8 %5
}

; Function Attrs: norecurse nounwind readonly uwtable
define i8 @_ZNK2b8mlERKS_(%class.b8* nocapture readonly %this, %class.b8* nocapture readonly dereferenceable(1) %o) #3 align 2 {
  %1 = getelementptr inbounds %class.b8, %class.b8* %this, i64 0, i32 0
  %2 = load i8, i8* %1, align 1, !tbaa !1
  %3 = getelementptr inbounds %class.b8, %class.b8* %o, i64 0, i32 0
  %4 = load i8, i8* %3, align 1, !tbaa !1
  %5 = and i8 %4, %2
  ret i8 %5
}

; Function Attrs: norecurse nounwind readonly uwtable
define i8 @_ZNK2b8miERKS_(%class.b8* nocapture readonly %this, %class.b8* nocapture readonly dereferenceable(1) %o) #3 align 2 {
  %1 = getelementptr inbounds %class.b8, %class.b8* %this, i64 0, i32 0
  %2 = load i8, i8* %1, align 1, !tbaa !1
  %3 = getelementptr inbounds %class.b8, %class.b8* %o, i64 0, i32 0
  %4 = load i8, i8* %3, align 1, !tbaa !1
  %5 = xor i8 %4, -1
  %6 = and i8 %2, %5
  ret i8 %6
}

; Function Attrs: norecurse nounwind readonly uwtable
define i8 @_ZNK2b8dvERKS_(%class.b8* nocapture readonly %this, %class.b8* nocapture readonly dereferenceable(1) %o) #3 align 2 {
  %1 = getelementptr inbounds %class.b8, %class.b8* %this, i64 0, i32 0
  %2 = load i8, i8* %1, align 1, !tbaa !1
  %3 = xor i8 %2, -1
  %4 = getelementptr inbounds %class.b8, %class.b8* %o, i64 0, i32 0
  %5 = load i8, i8* %4, align 1, !tbaa !1
  %6 = or i8 %5, %3
  ret i8 %6
}

; Function Attrs: norecurse nounwind readonly uwtable
define i8 @_ZNK2b8eoERKS_(%class.b8* nocapture readonly %this, %class.b8* nocapture readonly dereferenceable(1) %o) #3 align 2 {
  %1 = getelementptr inbounds %class.b8, %class.b8* %this, i64 0, i32 0
  %2 = load i8, i8* %1, align 1, !tbaa !1
  %3 = getelementptr inbounds %class.b8, %class.b8* %o, i64 0, i32 0
  %4 = load i8, i8* %3, align 1, !tbaa !1
  %5 = xor i8 %4, %2
  ret i8 %5
}

; Function Attrs: norecurse nounwind readonly uwtable
define i8 @_ZNK2b8lsERKS_(%class.b8* nocapture readonly %this, %class.b8* nocapture readonly dereferenceable(1) %o) #3 align 2 {
  %1 = getelementptr inbounds %class.b8, %class.b8* %this, i64 0, i32 0
  %2 = load i8, i8* %1, align 1, !tbaa !1
  %3 = zext i8 %2 to i32
  %4 = getelementptr inbounds %class.b8, %class.b8* %o, i64 0, i32 0
  %5 = load i8, i8* %4, align 1, !tbaa !1
  %6 = zext i8 %5 to i32
  %7 = shl i32 %3, %6
  %8 = trunc i32 %7 to i8
  ret i8 %8
}

; Function Attrs: norecurse nounwind readonly uwtable
define i8 @_ZNK2b8rsERKS_(%class.b8* nocapture readonly %this, %class.b8* nocapture readonly dereferenceable(1) %o) #3 align 2 {
  %1 = getelementptr inbounds %class.b8, %class.b8* %this, i64 0, i32 0
  %2 = load i8, i8* %1, align 1, !tbaa !1
  %3 = zext i8 %2 to i32
  %4 = getelementptr inbounds %class.b8, %class.b8* %o, i64 0, i32 0
  %5 = load i8, i8* %4, align 1, !tbaa !1
  %6 = zext i8 %5 to i32
  %7 = lshr i32 %3, %6
  %8 = trunc i32 %7 to i8
  ret i8 %8
}

; Function Attrs: norecurse nounwind readonly uwtable
define zeroext i1 @_ZNK2b8orERKS_(%class.b8* nocapture readonly %this, %class.b8* nocapture readonly dereferenceable(1) %o) #3 align 2 {
  %1 = getelementptr inbounds %class.b8, %class.b8* %this, i64 0, i32 0
  %2 = load i8, i8* %1, align 1, !tbaa !1
  %3 = getelementptr inbounds %class.b8, %class.b8* %o, i64 0, i32 0
  %4 = load i8, i8* %3, align 1, !tbaa !1
  %5 = or i8 %4, %2
  %6 = icmp ne i8 %5, 0
  ret i1 %6
}

; Function Attrs: norecurse nounwind readonly uwtable
define zeroext i1 @_ZNK2b8anERKS_(%class.b8* nocapture readonly %this, %class.b8* nocapture readonly dereferenceable(1) %o) #3 align 2 {
  %1 = getelementptr inbounds %class.b8, %class.b8* %this, i64 0, i32 0
  %2 = load i8, i8* %1, align 1, !tbaa !1
  %3 = icmp ne i8 %2, 0
  %4 = getelementptr inbounds %class.b8, %class.b8* %o, i64 0, i32 0
  %5 = load i8, i8* %4, align 1, !tbaa !1
  %6 = icmp ne i8 %5, 0
  %7 = and i1 %3, %6
  ret i1 %7
}

; Function Attrs: norecurse nounwind readonly uwtable
define zeroext i1 @_ZntRK2tf(i8* nocapture readonly dereferenceable(1) %self) #3 {
  %1 = load i8, i8* %self, align 1, !tbaa !5
  %2 = icmp ne i8 %1, 0
  ret i1 %2
}

; Function Attrs: norecurse nounwind readonly uwtable
define zeroext i8 @_ZcoRK2tf(i8* nocapture readonly dereferenceable(1) %self) #3 {
  %1 = load i8, i8* %self, align 1, !tbaa !5
  %2 = xor i8 %1, -1
  ret i8 %2
}

; Function Attrs: norecurse nounwind readonly uwtable
define zeroext i8 @_ZplRK2tfS1_(i8* nocapture readonly dereferenceable(1) %l, i8* nocapture readonly dereferenceable(1) %r) #3 {
  %1 = load i8, i8* %l, align 1, !tbaa !5
  %2 = load i8, i8* %r, align 1, !tbaa !5
  %3 = or i8 %2, %1
  ret i8 %3
}

; Function Attrs: norecurse nounwind readonly uwtable
define zeroext i8 @_ZmiRK2tfS1_(i8* nocapture readonly dereferenceable(1) %l, i8* nocapture readonly dereferenceable(1) %r) #3 {
  %1 = load i8, i8* %l, align 1, !tbaa !5
  %2 = load i8, i8* %r, align 1, !tbaa !5
  %3 = xor i8 %2, -1
  %4 = and i8 %1, %3
  ret i8 %4
}

; Function Attrs: norecurse nounwind readonly uwtable
define zeroext i8 @_ZmlRK2tfS1_(i8* nocapture readonly dereferenceable(1) %l, i8* nocapture readonly dereferenceable(1) %r) #3 {
  %1 = load i8, i8* %l, align 1, !tbaa !5
  %2 = load i8, i8* %r, align 1, !tbaa !5
  %3 = and i8 %2, %1
  ret i8 %3
}

; Function Attrs: norecurse nounwind readonly uwtable
define zeroext i8 @_ZdvRK2tfS1_(i8* nocapture readonly dereferenceable(1) %l, i8* nocapture readonly dereferenceable(1) %r) #3 {
  %1 = load i8, i8* %l, align 1, !tbaa !5
  %2 = xor i8 %1, -1
  %3 = load i8, i8* %r, align 1, !tbaa !5
  %4 = or i8 %3, %2
  ret i8 %4
}

; Function Attrs: norecurse nounwind readonly uwtable
define zeroext i8 @_ZeoRK2tfS1_(i8* nocapture readonly dereferenceable(1) %l, i8* nocapture readonly dereferenceable(1) %r) #3 {
  %1 = load i8, i8* %l, align 1, !tbaa !5
  %2 = load i8, i8* %r, align 1, !tbaa !5
  %3 = xor i8 %2, %1
  ret i8 %3
}

; Function Attrs: norecurse nounwind uwtable
define nonnull dereferenceable(1) i8* @_ZpLR2tfRKS_(i8* dereferenceable(1) %l, i8* nocapture readonly dereferenceable(1) %r) #4 {
  %1 = load i8, i8* %l, align 1, !tbaa !5
  %2 = load i8, i8* %r, align 1, !tbaa !5
  %3 = or i8 %2, %1
  store i8 %3, i8* %l, align 1, !tbaa !5
  ret i8* %l
}

; Function Attrs: norecurse nounwind uwtable
define nonnull dereferenceable(1) i8* @_ZmIR2tfRKS_(i8* dereferenceable(1) %l, i8* nocapture readonly dereferenceable(1) %r) #4 {
  %1 = load i8, i8* %l, align 1, !tbaa !5
  %2 = load i8, i8* %r, align 1, !tbaa !5
  %3 = xor i8 %2, -1
  %4 = and i8 %1, %3
  store i8 %4, i8* %l, align 1, !tbaa !5
  ret i8* %l
}

; Function Attrs: norecurse nounwind uwtable
define nonnull dereferenceable(1) i8* @_ZmLR2tfRKS_(i8* dereferenceable(1) %l, i8* nocapture readonly dereferenceable(1) %r) #4 {
  %1 = load i8, i8* %l, align 1, !tbaa !5
  %2 = load i8, i8* %r, align 1, !tbaa !5
  %3 = and i8 %2, %1
  store i8 %3, i8* %l, align 1, !tbaa !5
  ret i8* %l
}

; Function Attrs: norecurse nounwind uwtable
define nonnull dereferenceable(1) i8* @_ZdVR2tfRKS_(i8* dereferenceable(1) %l, i8* nocapture readonly dereferenceable(1) %r) #4 {
  %1 = load i8, i8* %l, align 1, !tbaa !5
  %2 = xor i8 %1, -1
  %3 = load i8, i8* %r, align 1, !tbaa !5
  %4 = or i8 %3, %2
  store i8 %4, i8* %l, align 1, !tbaa !5
  ret i8* %l
}

; Function Attrs: norecurse nounwind uwtable
define nonnull dereferenceable(1) i8* @_ZeOR2tfRKS_(i8* dereferenceable(1) %l, i8* nocapture readonly dereferenceable(1) %r) #4 {
  %1 = load i8, i8* %l, align 1, !tbaa !5
  %2 = load i8, i8* %r, align 1, !tbaa !5
  %3 = xor i8 %2, %1
  store i8 %3, i8* %l, align 1, !tbaa !5
  ret i8* %l
}

; Function Attrs: norecurse uwtable
define i32 @main() #5 {
  %a = alloca [1048832 x %class.b8], align 16
  %b = alloca [1048832 x %class.b8], align 16
  %c = alloca [1048832 x %class.b8], align 16
  %1 = getelementptr inbounds [1048832 x %class.b8], [1048832 x %class.b8]* %a, i64 0, i64 0, i32 0
  call void @llvm.lifetime.start(i64 1048832, i8* %1) #2
  %2 = getelementptr inbounds [1048832 x %class.b8], [1048832 x %class.b8]* %b, i64 0, i64 0, i32 0
  call void @llvm.lifetime.start(i64 1048832, i8* %2) #2
  %3 = getelementptr inbounds [1048832 x %class.b8], [1048832 x %class.b8]* %c, i64 0, i64 0, i32 0
  call void @llvm.lifetime.start(i64 1048832, i8* %3) #2
  br label %5

.preheader:                                       ; preds = %5
  %4 = tail call i32 @rand() #2
  br label %min.iters.checked

; <label>:5                                       ; preds = %5, %0
  %indvars.iv8 = phi i64 [ 0, %0 ], [ %indvars.iv.next9, %5 ]
  %6 = tail call i32 @rand() #2
  %7 = trunc i32 %6 to i8
  %8 = getelementptr inbounds [1048832 x %class.b8], [1048832 x %class.b8]* %a, i64 0, i64 %indvars.iv8, i32 0
  store i8 %7, i8* %8, align 1, !tbaa !1
  %9 = tail call i32 @rand() #2
  %10 = trunc i32 %9 to i8
  %11 = getelementptr inbounds [1048832 x %class.b8], [1048832 x %class.b8]* %b, i64 0, i64 %indvars.iv8, i32 0
  store i8 %10, i8* %11, align 1, !tbaa !1
  %indvars.iv.next9 = add nuw nsw i64 %indvars.iv8, 1
  %exitcond10 = icmp eq i64 %indvars.iv.next9, 1048832
  br i1 %exitcond10, label %.preheader, label %5

; <label>:12                                      ; preds = %middle.block
  %.lcssa = phi i32 [ %73, %middle.block ]
  %13 = tail call dereferenceable(272) %"class.std::basic_ostream"* @_ZNSolsEi(%"class.std::basic_ostream"* nonnull @_ZSt4cout, i32 %.lcssa)
  %14 = bitcast %"class.std::basic_ostream"* %13 to i8**
  %15 = load i8*, i8** %14, align 8, !tbaa !7
  %16 = getelementptr i8, i8* %15, i64 -24
  %17 = bitcast i8* %16 to i64*
  %18 = load i64, i64* %17, align 8
  %19 = bitcast %"class.std::basic_ostream"* %13 to i8*
  %20 = getelementptr inbounds i8, i8* %19, i64 %18
  %21 = getelementptr inbounds i8, i8* %20, i64 240
  %22 = bitcast i8* %21 to %"class.std::ctype"**
  %23 = load %"class.std::ctype"*, %"class.std::ctype"** %22, align 8, !tbaa !9
  %24 = icmp eq %"class.std::ctype"* %23, null
  br i1 %24, label %25, label %_ZSt13__check_facetISt5ctypeIcEERKT_PS3_.exit

; <label>:25                                      ; preds = %12
  tail call void @_ZSt16__throw_bad_castv() #9
  unreachable

_ZSt13__check_facetISt5ctypeIcEERKT_PS3_.exit:    ; preds = %12
  %26 = getelementptr inbounds %"class.std::ctype", %"class.std::ctype"* %23, i64 0, i32 8
  %27 = load i8, i8* %26, align 8, !tbaa !13
  %28 = icmp eq i8 %27, 0
  br i1 %28, label %32, label %29

; <label>:29                                      ; preds = %_ZSt13__check_facetISt5ctypeIcEERKT_PS3_.exit
  %30 = getelementptr inbounds %"class.std::ctype", %"class.std::ctype"* %23, i64 0, i32 9, i64 10
  %31 = load i8, i8* %30, align 1, !tbaa !15
  br label %_ZNKSt5ctypeIcE5widenEc.exit

; <label>:32                                      ; preds = %_ZSt13__check_facetISt5ctypeIcEERKT_PS3_.exit
  tail call void @_ZNKSt5ctypeIcE13_M_widen_initEv(%"class.std::ctype"* nonnull %23)
  %33 = bitcast %"class.std::ctype"* %23 to i8 (%"class.std::ctype"*, i8)***
  %34 = load i8 (%"class.std::ctype"*, i8)**, i8 (%"class.std::ctype"*, i8)*** %33, align 8, !tbaa !7
  %35 = getelementptr inbounds i8 (%"class.std::ctype"*, i8)*, i8 (%"class.std::ctype"*, i8)** %34, i64 6
  %36 = load i8 (%"class.std::ctype"*, i8)*, i8 (%"class.std::ctype"*, i8)** %35, align 8
  %37 = tail call signext i8 %36(%"class.std::ctype"* nonnull %23, i8 signext 10)
  br label %_ZNKSt5ctypeIcE5widenEc.exit

_ZNKSt5ctypeIcE5widenEc.exit:                     ; preds = %29, %32
  %.0.i = phi i8 [ %31, %29 ], [ %37, %32 ]
  %38 = tail call dereferenceable(272) %"class.std::basic_ostream"* @_ZNSo3putEc(%"class.std::basic_ostream"* nonnull %13, i8 signext %.0.i)
  %39 = tail call dereferenceable(272) %"class.std::basic_ostream"* @_ZNSo5flushEv(%"class.std::basic_ostream"* nonnull %38)
  %40 = getelementptr inbounds [1048832 x %class.b8], [1048832 x %class.b8]* %c, i64 0, i64 1048831, i32 0
  %41 = load i8, i8* %40, align 1, !tbaa !1
  %42 = zext i8 %41 to i32
  call void @llvm.lifetime.end(i64 1048832, i8* nonnull %3) #2
  call void @llvm.lifetime.end(i64 1048832, i8* nonnull %2) #2
  call void @llvm.lifetime.end(i64 1048832, i8* nonnull %1) #2
  ret i32 %42

min.iters.checked:                                ; preds = %middle.block, %.preheader
  %43 = phi i32 [ %4, %.preheader ], [ %73, %middle.block ]
  %j.05 = phi i32 [ 0, %.preheader ], [ %72, %middle.block ]
  %f.04 = phi i8 [ 1, %.preheader ], [ %.f.0, %middle.block ]
  %44 = icmp sgt i32 %43, 666
  %45 = or i8 %f.04, 2
  %.f.0 = select i1 %44, i8 %45, i8 %f.04
  %46 = lshr i32 %j.05, 7
  %47 = zext i8 %.f.0 to i32
  %48 = add nuw nsw i32 %47, %46
  %broadcast.splatinsert12 = insertelement <16 x i32> undef, i32 %48, i32 0
  %broadcast.splat13 = shufflevector <16 x i32> %broadcast.splatinsert12, <16 x i32> undef, <16 x i32> zeroinitializer
  br label %vector.body

vector.body:                                      ; preds = %vector.body, %min.iters.checked
  %index = phi i64 [ 0, %min.iters.checked ], [ %index.next.1, %vector.body ]
  %49 = getelementptr inbounds [1048832 x %class.b8], [1048832 x %class.b8]* %a, i64 0, i64 %index, i32 0
  %50 = bitcast i8* %49 to <16 x i8>*
  %wide.load = load <16 x i8>, <16 x i8>* %50, align 16, !tbaa !1
  %51 = shl <16 x i8> %wide.load, <i8 3, i8 3, i8 3, i8 3, i8 3, i8 3, i8 3, i8 3, i8 3, i8 3, i8 3, i8 3, i8 3, i8 3, i8 3, i8 3>
  %52 = getelementptr inbounds [1048832 x %class.b8], [1048832 x %class.b8]* %b, i64 0, i64 %index, i32 0
  %53 = bitcast i8* %52 to <16 x i8>*
  %wide.load11 = load <16 x i8>, <16 x i8>* %53, align 16, !tbaa !1
  %54 = and <16 x i8> %wide.load11, %51
  %55 = zext <16 x i8> %54 to <16 x i32>
  %56 = add nuw nsw <16 x i32> %broadcast.splat13, %55
  %57 = trunc <16 x i32> %56 to <16 x i8>
  %58 = getelementptr inbounds [1048832 x %class.b8], [1048832 x %class.b8]* %c, i64 0, i64 %index, i32 0
  %59 = bitcast i8* %58 to <16 x i8>*
  store <16 x i8> %57, <16 x i8>* %59, align 16, !tbaa !1
  %index.next = or i64 %index, 16
  %60 = getelementptr inbounds [1048832 x %class.b8], [1048832 x %class.b8]* %a, i64 0, i64 %index.next, i32 0
  %61 = bitcast i8* %60 to <16 x i8>*
  %wide.load.1 = load <16 x i8>, <16 x i8>* %61, align 16, !tbaa !1
  %62 = shl <16 x i8> %wide.load.1, <i8 3, i8 3, i8 3, i8 3, i8 3, i8 3, i8 3, i8 3, i8 3, i8 3, i8 3, i8 3, i8 3, i8 3, i8 3, i8 3>
  %63 = getelementptr inbounds [1048832 x %class.b8], [1048832 x %class.b8]* %b, i64 0, i64 %index.next, i32 0
  %64 = bitcast i8* %63 to <16 x i8>*
  %wide.load11.1 = load <16 x i8>, <16 x i8>* %64, align 16, !tbaa !1
  %65 = and <16 x i8> %wide.load11.1, %62
  %66 = zext <16 x i8> %65 to <16 x i32>
  %67 = add nuw nsw <16 x i32> %broadcast.splat13, %66
  %68 = trunc <16 x i32> %67 to <16 x i8>
  %69 = getelementptr inbounds [1048832 x %class.b8], [1048832 x %class.b8]* %c, i64 0, i64 %index.next, i32 0
  %70 = bitcast i8* %69 to <16 x i8>*
  store <16 x i8> %68, <16 x i8>* %70, align 16, !tbaa !1
  %index.next.1 = add nsw i64 %index, 32
  %71 = icmp eq i64 %index.next.1, 1048832
  br i1 %71, label %middle.block, label %vector.body, !llvm.loop !16

middle.block:                                     ; preds = %vector.body
  %72 = add nuw nsw i32 %j.05, 1
  %73 = tail call i32 @rand() #2
  %exitcond7 = icmp eq i32 %72, 512
  br i1 %exitcond7, label %12, label %min.iters.checked
}

; Function Attrs: argmemonly nounwind
declare void @llvm.lifetime.start(i64, i8* nocapture) #6

; Function Attrs: nounwind
declare i32 @rand() #1

; Function Attrs: argmemonly nounwind
declare void @llvm.lifetime.end(i64, i8* nocapture) #6

declare dereferenceable(272) %"class.std::basic_ostream"* @_ZNSolsEi(%"class.std::basic_ostream"*, i32) #0

declare dereferenceable(272) %"class.std::basic_ostream"* @_ZNSo3putEc(%"class.std::basic_ostream"*, i8 signext) #0

declare dereferenceable(272) %"class.std::basic_ostream"* @_ZNSo5flushEv(%"class.std::basic_ostream"*) #0

; Function Attrs: noreturn
declare void @_ZSt16__throw_bad_castv() #7

declare void @_ZNKSt5ctypeIcE13_M_widen_initEv(%"class.std::ctype"*) #0

; Function Attrs: uwtable
define internal void @_GLOBAL__sub_I_b8.cpp() #8 section ".text.startup" {
  tail call void @_ZNSt8ios_base4InitC1Ev(%"class.std::ios_base::Init"* nonnull @_ZStL8__ioinit)
  %1 = tail call i32 @__cxa_atexit(void (i8*)* bitcast (void (%"class.std::ios_base::Init"*)* @_ZNSt8ios_base4InitD1Ev to void (i8*)*), i8* getelementptr inbounds (%"class.std::ios_base::Init", %"class.std::ios_base::Init"* @_ZStL8__ioinit, i64 0, i32 0), i8* nonnull @__dso_handle) #2
  ret void
}

attributes #0 = { "disable-tail-calls"="false" "less-precise-fpmad"="false" "no-frame-pointer-elim"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+fxsr,+mmx,+sse,+sse2" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #1 = { nounwind "disable-tail-calls"="false" "less-precise-fpmad"="false" "no-frame-pointer-elim"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+fxsr,+mmx,+sse,+sse2" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #2 = { nounwind }
attributes #3 = { norecurse nounwind readonly uwtable "disable-tail-calls"="false" "less-precise-fpmad"="false" "no-frame-pointer-elim"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+fxsr,+mmx,+sse,+sse2" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #4 = { norecurse nounwind uwtable "disable-tail-calls"="false" "less-precise-fpmad"="false" "no-frame-pointer-elim"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+fxsr,+mmx,+sse,+sse2" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #5 = { norecurse uwtable "disable-tail-calls"="false" "less-precise-fpmad"="false" "no-frame-pointer-elim"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+fxsr,+mmx,+sse,+sse2" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #6 = { argmemonly nounwind }
attributes #7 = { noreturn "disable-tail-calls"="false" "less-precise-fpmad"="false" "no-frame-pointer-elim"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+fxsr,+mmx,+sse,+sse2" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #8 = { uwtable "disable-tail-calls"="false" "less-precise-fpmad"="false" "no-frame-pointer-elim"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+fxsr,+mmx,+sse,+sse2" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #9 = { noreturn }

!llvm.ident = !{!0}

!0 = !{!"clang version 3.8.0-2ubuntu4 (tags/RELEASE_380/final)"}
!1 = !{!2, !3, i64 0}
!2 = !{!"_ZTS2b8", !3, i64 0}
!3 = !{!"omnipotent char", !4, i64 0}
!4 = !{!"Simple C/C++ TBAA"}
!5 = !{!6, !6, i64 0}
!6 = !{!"_ZTS2tf", !3, i64 0}
!7 = !{!8, !8, i64 0}
!8 = !{!"vtable pointer", !4, i64 0}
!9 = !{!10, !11, i64 240}
!10 = !{!"_ZTSSt9basic_iosIcSt11char_traitsIcEE", !11, i64 216, !3, i64 224, !12, i64 225, !11, i64 232, !11, i64 240, !11, i64 248, !11, i64 256}
!11 = !{!"any pointer", !3, i64 0}
!12 = !{!"bool", !3, i64 0}
!13 = !{!14, !3, i64 56}
!14 = !{!"_ZTSSt5ctypeIcE", !11, i64 16, !12, i64 24, !11, i64 32, !11, i64 40, !11, i64 48, !3, i64 56, !3, i64 57, !3, i64 313, !3, i64 569}
!15 = !{!3, !3, i64 0}
!16 = distinct !{!16, !17, !18}
!17 = !{!"llvm.loop.vectorize.width", i32 1}
!18 = !{!"llvm.loop.interleave.count", i32 1}
