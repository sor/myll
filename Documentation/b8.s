	.text
	.file	"b8.ll"
	.globl	_ZNK2b86is_nanEv
	.align	16, 0x90
	.type	_ZNK2b86is_nanEv,@function
_ZNK2b86is_nanEv:                       # @_ZNK2b86is_nanEv
	.cfi_startproc
# BB#0:
	movzbl	(%rdi), %eax
	cmpl	$255, %eax
	sete	%al
	retq
.Lfunc_end0:
	.size	_ZNK2b86is_nanEv, .Lfunc_end0-_ZNK2b86is_nanEv
	.cfi_endproc

	.globl	_ZN2b87set_nanEv
	.align	16, 0x90
	.type	_ZN2b87set_nanEv,@function
_ZN2b87set_nanEv:                       # @_ZN2b87set_nanEv
	.cfi_startproc
# BB#0:
	movb	$-1, (%rdi)
	retq
.Lfunc_end1:
	.size	_ZN2b87set_nanEv, .Lfunc_end1-_ZN2b87set_nanEv
	.cfi_endproc

	.globl	_ZNK2b8ntEv
	.align	16, 0x90
	.type	_ZNK2b8ntEv,@function
_ZNK2b8ntEv:                            # @_ZNK2b8ntEv
	.cfi_startproc
# BB#0:
	cmpb	$0, (%rdi)
	setne	%al
	retq
.Lfunc_end2:
	.size	_ZNK2b8ntEv, .Lfunc_end2-_ZNK2b8ntEv
	.cfi_endproc

	.globl	_ZNK2b8coEv
	.align	16, 0x90
	.type	_ZNK2b8coEv,@function
_ZNK2b8coEv:                            # @_ZNK2b8coEv
	.cfi_startproc
# BB#0:
	movb	(%rdi), %al
	notb	%al
	retq
.Lfunc_end3:
	.size	_ZNK2b8coEv, .Lfunc_end3-_ZNK2b8coEv
	.cfi_endproc

	.globl	_ZNK2b8plERKS_
	.align	16, 0x90
	.type	_ZNK2b8plERKS_,@function
_ZNK2b8plERKS_:                         # @_ZNK2b8plERKS_
	.cfi_startproc
# BB#0:
	movb	(%rsi), %al
	orb	(%rdi), %al
	retq
.Lfunc_end4:
	.size	_ZNK2b8plERKS_, .Lfunc_end4-_ZNK2b8plERKS_
	.cfi_endproc

	.globl	_ZNK2b8mlERKS_
	.align	16, 0x90
	.type	_ZNK2b8mlERKS_,@function
_ZNK2b8mlERKS_:                         # @_ZNK2b8mlERKS_
	.cfi_startproc
# BB#0:
	movb	(%rsi), %al
	andb	(%rdi), %al
	retq
.Lfunc_end5:
	.size	_ZNK2b8mlERKS_, .Lfunc_end5-_ZNK2b8mlERKS_
	.cfi_endproc

	.globl	_ZNK2b8miERKS_
	.align	16, 0x90
	.type	_ZNK2b8miERKS_,@function
_ZNK2b8miERKS_:                         # @_ZNK2b8miERKS_
	.cfi_startproc
# BB#0:
	movb	(%rsi), %al
	notb	%al
	andb	(%rdi), %al
	retq
.Lfunc_end6:
	.size	_ZNK2b8miERKS_, .Lfunc_end6-_ZNK2b8miERKS_
	.cfi_endproc

	.globl	_ZNK2b8dvERKS_
	.align	16, 0x90
	.type	_ZNK2b8dvERKS_,@function
_ZNK2b8dvERKS_:                         # @_ZNK2b8dvERKS_
	.cfi_startproc
# BB#0:
	movb	(%rdi), %al
	notb	%al
	orb	(%rsi), %al
	retq
.Lfunc_end7:
	.size	_ZNK2b8dvERKS_, .Lfunc_end7-_ZNK2b8dvERKS_
	.cfi_endproc

	.globl	_ZNK2b8eoERKS_
	.align	16, 0x90
	.type	_ZNK2b8eoERKS_,@function
_ZNK2b8eoERKS_:                         # @_ZNK2b8eoERKS_
	.cfi_startproc
# BB#0:
	movb	(%rsi), %al
	xorb	(%rdi), %al
	retq
.Lfunc_end8:
	.size	_ZNK2b8eoERKS_, .Lfunc_end8-_ZNK2b8eoERKS_
	.cfi_endproc

	.globl	_ZNK2b8lsERKS_
	.align	16, 0x90
	.type	_ZNK2b8lsERKS_,@function
_ZNK2b8lsERKS_:                         # @_ZNK2b8lsERKS_
	.cfi_startproc
# BB#0:
	movzbl	(%rdi), %eax
	movb	(%rsi), %cl
	shll	%cl, %eax
	retq
.Lfunc_end9:
	.size	_ZNK2b8lsERKS_, .Lfunc_end9-_ZNK2b8lsERKS_
	.cfi_endproc

	.globl	_ZNK2b8rsERKS_
	.align	16, 0x90
	.type	_ZNK2b8rsERKS_,@function
_ZNK2b8rsERKS_:                         # @_ZNK2b8rsERKS_
	.cfi_startproc
# BB#0:
	movzbl	(%rdi), %eax
	movb	(%rsi), %cl
	shrl	%cl, %eax
	retq
.Lfunc_end10:
	.size	_ZNK2b8rsERKS_, .Lfunc_end10-_ZNK2b8rsERKS_
	.cfi_endproc

	.globl	_ZNK2b8orERKS_
	.align	16, 0x90
	.type	_ZNK2b8orERKS_,@function
_ZNK2b8orERKS_:                         # @_ZNK2b8orERKS_
	.cfi_startproc
# BB#0:
	movb	(%rsi), %al
	orb	(%rdi), %al
	setne	%al
	retq
.Lfunc_end11:
	.size	_ZNK2b8orERKS_, .Lfunc_end11-_ZNK2b8orERKS_
	.cfi_endproc

	.globl	_ZNK2b8anERKS_
	.align	16, 0x90
	.type	_ZNK2b8anERKS_,@function
_ZNK2b8anERKS_:                         # @_ZNK2b8anERKS_
	.cfi_startproc
# BB#0:
	cmpb	$0, (%rdi)
	setne	%cl
	cmpb	$0, (%rsi)
	setne	%al
	andb	%cl, %al
	retq
.Lfunc_end12:
	.size	_ZNK2b8anERKS_, .Lfunc_end12-_ZNK2b8anERKS_
	.cfi_endproc

	.globl	_ZntRK2tf
	.align	16, 0x90
	.type	_ZntRK2tf,@function
_ZntRK2tf:                              # @_ZntRK2tf
	.cfi_startproc
# BB#0:
	cmpb	$0, (%rdi)
	setne	%al
	retq
.Lfunc_end13:
	.size	_ZntRK2tf, .Lfunc_end13-_ZntRK2tf
	.cfi_endproc

	.globl	_ZcoRK2tf
	.align	16, 0x90
	.type	_ZcoRK2tf,@function
_ZcoRK2tf:                              # @_ZcoRK2tf
	.cfi_startproc
# BB#0:
	movzbl	(%rdi), %eax
	xorl	$255, %eax
	retq
.Lfunc_end14:
	.size	_ZcoRK2tf, .Lfunc_end14-_ZcoRK2tf
	.cfi_endproc

	.globl	_ZplRK2tfS1_
	.align	16, 0x90
	.type	_ZplRK2tfS1_,@function
_ZplRK2tfS1_:                           # @_ZplRK2tfS1_
	.cfi_startproc
# BB#0:
	movb	(%rsi), %al
	orb	(%rdi), %al
	movzbl	%al, %eax
	retq
.Lfunc_end15:
	.size	_ZplRK2tfS1_, .Lfunc_end15-_ZplRK2tfS1_
	.cfi_endproc

	.globl	_ZmiRK2tfS1_
	.align	16, 0x90
	.type	_ZmiRK2tfS1_,@function
_ZmiRK2tfS1_:                           # @_ZmiRK2tfS1_
	.cfi_startproc
# BB#0:
	movb	(%rsi), %al
	notb	%al
	andb	(%rdi), %al
	movzbl	%al, %eax
	retq
.Lfunc_end16:
	.size	_ZmiRK2tfS1_, .Lfunc_end16-_ZmiRK2tfS1_
	.cfi_endproc

	.globl	_ZmlRK2tfS1_
	.align	16, 0x90
	.type	_ZmlRK2tfS1_,@function
_ZmlRK2tfS1_:                           # @_ZmlRK2tfS1_
	.cfi_startproc
# BB#0:
	movb	(%rsi), %al
	andb	(%rdi), %al
	movzbl	%al, %eax
	retq
.Lfunc_end17:
	.size	_ZmlRK2tfS1_, .Lfunc_end17-_ZmlRK2tfS1_
	.cfi_endproc

	.globl	_ZdvRK2tfS1_
	.align	16, 0x90
	.type	_ZdvRK2tfS1_,@function
_ZdvRK2tfS1_:                           # @_ZdvRK2tfS1_
	.cfi_startproc
# BB#0:
	movb	(%rdi), %al
	notb	%al
	orb	(%rsi), %al
	movzbl	%al, %eax
	retq
.Lfunc_end18:
	.size	_ZdvRK2tfS1_, .Lfunc_end18-_ZdvRK2tfS1_
	.cfi_endproc

	.globl	_ZeoRK2tfS1_
	.align	16, 0x90
	.type	_ZeoRK2tfS1_,@function
_ZeoRK2tfS1_:                           # @_ZeoRK2tfS1_
	.cfi_startproc
# BB#0:
	movb	(%rsi), %al
	xorb	(%rdi), %al
	movzbl	%al, %eax
	retq
.Lfunc_end19:
	.size	_ZeoRK2tfS1_, .Lfunc_end19-_ZeoRK2tfS1_
	.cfi_endproc

	.globl	_ZpLR2tfRKS_
	.align	16, 0x90
	.type	_ZpLR2tfRKS_,@function
_ZpLR2tfRKS_:                           # @_ZpLR2tfRKS_
	.cfi_startproc
# BB#0:
	movb	(%rsi), %al
	orb	%al, (%rdi)
	movq	%rdi, %rax
	retq
.Lfunc_end20:
	.size	_ZpLR2tfRKS_, .Lfunc_end20-_ZpLR2tfRKS_
	.cfi_endproc

	.globl	_ZmIR2tfRKS_
	.align	16, 0x90
	.type	_ZmIR2tfRKS_,@function
_ZmIR2tfRKS_:                           # @_ZmIR2tfRKS_
	.cfi_startproc
# BB#0:
	movb	(%rsi), %al
	notb	%al
	andb	%al, (%rdi)
	movq	%rdi, %rax
	retq
.Lfunc_end21:
	.size	_ZmIR2tfRKS_, .Lfunc_end21-_ZmIR2tfRKS_
	.cfi_endproc

	.globl	_ZmLR2tfRKS_
	.align	16, 0x90
	.type	_ZmLR2tfRKS_,@function
_ZmLR2tfRKS_:                           # @_ZmLR2tfRKS_
	.cfi_startproc
# BB#0:
	movb	(%rsi), %al
	andb	%al, (%rdi)
	movq	%rdi, %rax
	retq
.Lfunc_end22:
	.size	_ZmLR2tfRKS_, .Lfunc_end22-_ZmLR2tfRKS_
	.cfi_endproc

	.globl	_ZdVR2tfRKS_
	.align	16, 0x90
	.type	_ZdVR2tfRKS_,@function
_ZdVR2tfRKS_:                           # @_ZdVR2tfRKS_
	.cfi_startproc
# BB#0:
	movb	(%rdi), %al
	notb	%al
	orb	(%rsi), %al
	movb	%al, (%rdi)
	movq	%rdi, %rax
	retq
.Lfunc_end23:
	.size	_ZdVR2tfRKS_, .Lfunc_end23-_ZdVR2tfRKS_
	.cfi_endproc

	.globl	_ZeOR2tfRKS_
	.align	16, 0x90
	.type	_ZeOR2tfRKS_,@function
_ZeOR2tfRKS_:                           # @_ZeOR2tfRKS_
	.cfi_startproc
# BB#0:
	movb	(%rsi), %al
	xorb	%al, (%rdi)
	movq	%rdi, %rax
	retq
.Lfunc_end24:
	.size	_ZeOR2tfRKS_, .Lfunc_end24-_ZeOR2tfRKS_
	.cfi_endproc

	.section	.rodata.cst16,"aM",@progbits,16
	.align	16
.LCPI25_0:
	.zero	16,248
.LCPI25_1:
	.byte	255                     # 0xff
	.byte	0                       # 0x0
	.byte	0                       # 0x0
	.byte	0                       # 0x0
	.byte	255                     # 0xff
	.byte	0                       # 0x0
	.byte	0                       # 0x0
	.byte	0                       # 0x0
	.byte	255                     # 0xff
	.byte	0                       # 0x0
	.byte	0                       # 0x0
	.byte	0                       # 0x0
	.byte	255                     # 0xff
	.byte	0                       # 0x0
	.byte	0                       # 0x0
	.byte	0                       # 0x0
	.text
	.globl	main
	.align	16, 0x90
	.type	main,@function
main:                                   # @main
	.cfi_startproc
# BB#0:
	pushq	%rbp
.Ltmp0:
	.cfi_def_cfa_offset 16
	pushq	%r14
.Ltmp1:
	.cfi_def_cfa_offset 24
	pushq	%rbx
.Ltmp2:
	.cfi_def_cfa_offset 32
	subq	$3146528, %rsp          # imm = 0x300320
.Ltmp3:
	.cfi_def_cfa_offset 3146560
.Ltmp4:
	.cfi_offset %rbx, -32
.Ltmp5:
	.cfi_offset %r14, -24
.Ltmp6:
	.cfi_offset %rbp, -16
	xorl	%ebx, %ebx
	.align	16, 0x90
.LBB25_1:                               # =>This Inner Loop Header: Depth=1
	callq	rand
	movb	%al, 2097696(%rsp,%rbx)
	callq	rand
	movb	%al, 1048864(%rsp,%rbx)
	incq	%rbx
	cmpq	$1048832, %rbx          # imm = 0x100100
	jne	.LBB25_1
# BB#2:                                 # %.preheader
	movb	$1, %bl
	xorl	%ebp, %ebp
	callq	rand
	.align	16, 0x90
.LBB25_3:                               # %min.iters.checked
                                        # =>This Loop Header: Depth=1
                                        #     Child Loop BB25_6 Depth 2
	cmpl	$666, %eax              # imm = 0x29A
	jle	.LBB25_5
# BB#4:                                 #   in Loop: Header=BB25_3 Depth=1
	orb	$2, %bl
.LBB25_5:                               # %min.iters.checked
                                        #   in Loop: Header=BB25_3 Depth=1
	movl	%ebp, %eax
	shrl	$7, %eax
	movzbl	%bl, %ecx
	addl	%eax, %ecx
	movd	%ecx, %xmm0
	pshufd	$0, %xmm0, %xmm0        # xmm0 = xmm0[0,0,0,0]
	movl	$16, %eax
	movdqa	.LCPI25_0(%rip), %xmm5  # xmm5 = [248,248,248,248,248,248,248,248,248,248,248,248,248,248,248,248]
	pxor	%xmm6, %xmm6
	movdqa	.LCPI25_1(%rip), %xmm7  # xmm7 = [255,0,0,0,255,0,0,0,255,0,0,0,255,0,0,0]
	.align	16, 0x90
.LBB25_6:                               # %vector.body
                                        #   Parent Loop BB25_3 Depth=1
                                        # =>  This Inner Loop Header: Depth=2
	movdqa	2097680(%rsp,%rax), %xmm1
	psllw	$3, %xmm1
	pand	%xmm5, %xmm1
	pand	1048848(%rsp,%rax), %xmm1
	movdqa	%xmm1, %xmm2
	punpckhbw	%xmm6, %xmm2    # xmm2 = xmm2[8],xmm6[8],xmm2[9],xmm6[9],xmm2[10],xmm6[10],xmm2[11],xmm6[11],xmm2[12],xmm6[12],xmm2[13],xmm6[13],xmm2[14],xmm6[14],xmm2[15],xmm6[15]
	movdqa	%xmm2, %xmm3
	punpckhwd	%xmm6, %xmm3    # xmm3 = xmm3[4],xmm6[4],xmm3[5],xmm6[5],xmm3[6],xmm6[6],xmm3[7],xmm6[7]
	punpcklwd	%xmm6, %xmm2    # xmm2 = xmm2[0],xmm6[0],xmm2[1],xmm6[1],xmm2[2],xmm6[2],xmm2[3],xmm6[3]
	punpcklbw	%xmm6, %xmm1    # xmm1 = xmm1[0],xmm6[0],xmm1[1],xmm6[1],xmm1[2],xmm6[2],xmm1[3],xmm6[3],xmm1[4],xmm6[4],xmm1[5],xmm6[5],xmm1[6],xmm6[6],xmm1[7],xmm6[7]
	movdqa	%xmm1, %xmm4
	punpckhwd	%xmm6, %xmm4    # xmm4 = xmm4[4],xmm6[4],xmm4[5],xmm6[5],xmm4[6],xmm6[6],xmm4[7],xmm6[7]
	punpcklwd	%xmm6, %xmm1    # xmm1 = xmm1[0],xmm6[0],xmm1[1],xmm6[1],xmm1[2],xmm6[2],xmm1[3],xmm6[3]
	paddd	%xmm0, %xmm1
	paddd	%xmm0, %xmm4
	paddd	%xmm0, %xmm2
	paddd	%xmm0, %xmm3
	pand	%xmm7, %xmm3
	pand	%xmm7, %xmm2
	packuswb	%xmm3, %xmm2
	pand	%xmm7, %xmm4
	pand	%xmm7, %xmm1
	packuswb	%xmm4, %xmm1
	packuswb	%xmm2, %xmm1
	movdqa	%xmm1, 16(%rsp,%rax)
	movdqa	2097696(%rsp,%rax), %xmm1
	psllw	$3, %xmm1
	pand	%xmm5, %xmm1
	pand	1048864(%rsp,%rax), %xmm1
	movdqa	%xmm1, %xmm2
	punpckhbw	%xmm6, %xmm2    # xmm2 = xmm2[8],xmm6[8],xmm2[9],xmm6[9],xmm2[10],xmm6[10],xmm2[11],xmm6[11],xmm2[12],xmm6[12],xmm2[13],xmm6[13],xmm2[14],xmm6[14],xmm2[15],xmm6[15]
	movdqa	%xmm2, %xmm3
	punpckhwd	%xmm6, %xmm3    # xmm3 = xmm3[4],xmm6[4],xmm3[5],xmm6[5],xmm3[6],xmm6[6],xmm3[7],xmm6[7]
	punpcklwd	%xmm6, %xmm2    # xmm2 = xmm2[0],xmm6[0],xmm2[1],xmm6[1],xmm2[2],xmm6[2],xmm2[3],xmm6[3]
	punpcklbw	%xmm6, %xmm1    # xmm1 = xmm1[0],xmm6[0],xmm1[1],xmm6[1],xmm1[2],xmm6[2],xmm1[3],xmm6[3],xmm1[4],xmm6[4],xmm1[5],xmm6[5],xmm1[6],xmm6[6],xmm1[7],xmm6[7]
	movdqa	%xmm1, %xmm4
	punpckhwd	%xmm6, %xmm4    # xmm4 = xmm4[4],xmm6[4],xmm4[5],xmm6[5],xmm4[6],xmm6[6],xmm4[7],xmm6[7]
	punpcklwd	%xmm6, %xmm1    # xmm1 = xmm1[0],xmm6[0],xmm1[1],xmm6[1],xmm1[2],xmm6[2],xmm1[3],xmm6[3]
	paddd	%xmm0, %xmm1
	paddd	%xmm0, %xmm4
	paddd	%xmm0, %xmm2
	paddd	%xmm0, %xmm3
	pand	%xmm7, %xmm3
	pand	%xmm7, %xmm2
	packuswb	%xmm3, %xmm2
	pand	%xmm7, %xmm4
	pand	%xmm7, %xmm1
	packuswb	%xmm4, %xmm1
	packuswb	%xmm2, %xmm1
	movdqa	%xmm1, 32(%rsp,%rax)
	addq	$32, %rax
	cmpq	$1048848, %rax          # imm = 0x100110
	jne	.LBB25_6
# BB#7:                                 # %middle.block
                                        #   in Loop: Header=BB25_3 Depth=1
	incl	%ebp
	callq	rand
	cmpl	$512, %ebp              # imm = 0x200
	jne	.LBB25_3
# BB#8:
	movl	$_ZSt4cout, %edi
	movl	%eax, %esi
	callq	_ZNSolsEi
	movq	%rax, %r14
	movq	(%r14), %rax
	movq	-24(%rax), %rax
	movq	240(%r14,%rax), %rbx
	testq	%rbx, %rbx
	je	.LBB25_13
# BB#9:                                 # %_ZSt13__check_facetISt5ctypeIcEERKT_PS3_.exit
	cmpb	$0, 56(%rbx)
	je	.LBB25_11
# BB#10:
	movb	67(%rbx), %al
	jmp	.LBB25_12
.LBB25_11:
	movq	%rbx, %rdi
	callq	_ZNKSt5ctypeIcE13_M_widen_initEv
	movq	(%rbx), %rax
	movl	$10, %esi
	movq	%rbx, %rdi
	callq	*48(%rax)
.LBB25_12:                              # %_ZNKSt5ctypeIcE5widenEc.exit
	movsbl	%al, %esi
	movq	%r14, %rdi
	callq	_ZNSo3putEc
	movq	%rax, %rdi
	callq	_ZNSo5flushEv
	movzbl	1048863(%rsp), %eax
	addq	$3146528, %rsp          # imm = 0x300320
	popq	%rbx
	popq	%r14
	popq	%rbp
	retq
.LBB25_13:
	callq	_ZSt16__throw_bad_castv
.Lfunc_end25:
	.size	main, .Lfunc_end25-main
	.cfi_endproc

	.section	.text.startup,"ax",@progbits
	.align	16, 0x90
	.type	_GLOBAL__sub_I_b8.cpp,@function
_GLOBAL__sub_I_b8.cpp:                  # @_GLOBAL__sub_I_b8.cpp
	.cfi_startproc
# BB#0:
	pushq	%rax
.Ltmp7:
	.cfi_def_cfa_offset 16
	movl	$_ZStL8__ioinit, %edi
	callq	_ZNSt8ios_base4InitC1Ev
	movl	$_ZNSt8ios_base4InitD1Ev, %edi
	movl	$_ZStL8__ioinit, %esi
	movl	$__dso_handle, %edx
	popq	%rax
	jmp	__cxa_atexit            # TAILCALL
.Lfunc_end26:
	.size	_GLOBAL__sub_I_b8.cpp, .Lfunc_end26-_GLOBAL__sub_I_b8.cpp
	.cfi_endproc

	.type	_ZStL8__ioinit,@object  # @_ZStL8__ioinit
	.local	_ZStL8__ioinit
	.comm	_ZStL8__ioinit,1,1
	.section	.init_array,"aw",@init_array
	.align	8
	.quad	_GLOBAL__sub_I_b8.cpp

	.ident	"clang version 3.8.0-2ubuntu4 (tags/RELEASE_380/final)"
	.section	".note.GNU-stack","",@progbits
