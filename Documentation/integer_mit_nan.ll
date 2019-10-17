; ModuleID = 'test.cpp'
target datalayout = "e-m:e-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-pc-linux-gnu"

%"class.std::ios_base::Init" = type { i8 }

@_ZStL8__ioinit = internal global %"class.std::ios_base::Init" zeroinitializer, align 1
@__dso_handle = external global i8
@llvm.global_ctors = appending global [1 x { i32, void ()*, i8* }] [{ i32, void ()*, i8* } { i32 65535, void ()* @_GLOBAL__sub_I_test.cpp, i8* null }]

declare void @_ZNSt8ios_base4InitC1Ev(%"class.std::ios_base::Init"*) #0

; Function Attrs: nounwind
declare void @_ZNSt8ios_base4InitD1Ev(%"class.std::ios_base::Init"*) #1

; Function Attrs: nounwind
declare i32 @__cxa_atexit(void (i8*)*, i8*, i8*) #2

; Function Attrs: norecurse nounwind readnone uwtable
define i32 @_Z6uitouij(i32 %i) #3 {
  ret i32 %i
}

; Function Attrs: norecurse nounwind readnone uwtable
define i32 @_Z6ustouit(i16 zeroext %s) #3 {
  %1 = zext i16 %s to i32
  ret i32 %1
}

; Function Attrs: norecurse nounwind readnone uwtable
define i32 @_Z6uctouih(i8 zeroext %c) #3 {
  %1 = zext i8 %c to i32
  ret i32 %1
}

; Function Attrs: norecurse nounwind readnone uwtable
define i32 @_Z5uitoij(i32 %i) #3 {
  ret i32 %i
}

; Function Attrs: norecurse nounwind readnone uwtable
define i32 @_Z5ustoit(i16 zeroext %s) #3 {
  %1 = zext i16 %s to i32
  ret i32 %1
}

; Function Attrs: norecurse nounwind readnone uwtable
define i32 @_Z5uctoih(i8 zeroext %c) #3 {
  %1 = zext i8 %c to i32
  ret i32 %1
}

; Function Attrs: norecurse nounwind readnone uwtable
define i32 @_Z4itoii(i32 %i) #3 {
  ret i32 %i
}

; Function Attrs: norecurse nounwind readnone uwtable
define i32 @_Z4stois(i16 signext %s) #3 {
  %1 = sext i16 %s to i32
  ret i32 %1
}

; Function Attrs: norecurse nounwind readnone uwtable
define i32 @_Z4ctoic(i8 signext %c) #3 {
  %1 = sext i8 %c to i32
  ret i32 %1
}

; Function Attrs: norecurse nounwind readnone uwtable
define i32 @_Z5itouii(i32 %i) #3 {
  ret i32 %i
}

; Function Attrs: norecurse nounwind readnone uwtable
define i32 @_Z5stouis(i16 signext %s) #3 {
  %1 = sext i16 %s to i32
  ret i32 %1
}

; Function Attrs: norecurse nounwind readnone uwtable
define i32 @_Z5ctouic(i8 signext %c) #3 {
  %1 = sext i8 %c to i32
  ret i32 %1
}

; Function Attrs: norecurse nounwind readnone uwtable
define i32 @main() #3 {
  ret i32 0
}

; Function Attrs: uwtable
define internal void @_GLOBAL__sub_I_test.cpp() #4 section ".text.startup" {
  tail call void @_ZNSt8ios_base4InitC1Ev(%"class.std::ios_base::Init"* nonnull @_ZStL8__ioinit)
  %1 = tail call i32 @__cxa_atexit(void (i8*)* bitcast (void (%"class.std::ios_base::Init"*)* @_ZNSt8ios_base4InitD1Ev to void (i8*)*), i8* getelementptr inbounds (%"class.std::ios_base::Init", %"class.std::ios_base::Init"* @_ZStL8__ioinit, i64 0, i32 0), i8* nonnull @__dso_handle) #2
  ret void
}

attributes #0 = { "disable-tail-calls"="false" "less-precise-fpmad"="false" "no-frame-pointer-elim"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+fxsr,+mmx,+sse,+sse2" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #1 = { nounwind "disable-tail-calls"="false" "less-precise-fpmad"="false" "no-frame-pointer-elim"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+fxsr,+mmx,+sse,+sse2" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #2 = { nounwind }
attributes #3 = { norecurse nounwind readnone uwtable "disable-tail-calls"="false" "less-precise-fpmad"="false" "no-frame-pointer-elim"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+fxsr,+mmx,+sse,+sse2" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #4 = { uwtable "disable-tail-calls"="false" "less-precise-fpmad"="false" "no-frame-pointer-elim"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+fxsr,+mmx,+sse,+sse2" "unsafe-fp-math"="false" "use-soft-float"="false" }

!llvm.ident = !{!0}

!0 = !{!"clang version 3.8.0-2ubuntu4 (tags/RELEASE_380/final)"}
