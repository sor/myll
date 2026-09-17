# Build all Myll core utilities in this directory.
#
# Run from this directory:
#   make
#   make clean

MAKEFLAGS += -j4

MYLL_ROOT ?= ../..
MYLL      := dotnet run --project $(MYLL_ROOT)/frontend -- -i '*.myll' -o gen

CXX      ?= g++
CXXFLAGS ?= -std=c++23 -Ithird_party -DCXXOPTS_NO_REGEX -O1 -Wall -Wextra -Wsuggest-override

RELEASE_FLAGS ?= -O2 -s
DEBUG_FLAGS   ?= -Og -g

# Tools split into a shared logic module (coreutil_*.myll), a thin standalone
# wrapper, and the JanSordid::Coreutil shared library.
SPLIT_TOOLS := find cat basename dirname echo head wc grep ls yes pwd

# Trivial tools (true, false) are compiled straight from their wrapper .myll.
TOOLS       := $(filter-out $(SPLIT_TOOLS),$(basename $(filter-out coreutil_%.myll coreutil.myll,$(wildcard *.myll))))
BINS        := $(addprefix gen/,$(TOOLS))
ALL_BINS    := $(BINS) $(addprefix gen/,$(SPLIT_TOOLS)) gen/coreutil

.PHONY: all clean release debug

all: $(ALL_BINS)

# Generate C++ sources for all .myll files in one Myll invocation.
# Local helper headers are copied next to the generated files so the compiler
# finds them without needing the source directory on the include path.
gen/.stamp: $(wildcard *.myll) $(wildcard *.hpp)
	@mkdir -p gen
	$(MYLL)
	@for h in $(wildcard *.hpp); do cp -f "$$h" gen/; done
	@touch gen/.stamp

# Split tools: wrapper + logic + shared library.
$(addprefix gen/,$(SPLIT_TOOLS)): gen/%: gen/.stamp
	$(CXX) $(CXXFLAGS) gen/$*.cpp gen/coreutil_$*.cpp gen/coreutil_lib.cpp -o $@

# Combined multi-call binary: dispatcher + all tool logics + shared library.
COREUTIL_SRCS := coreutil.cpp $(foreach t,$(SPLIT_TOOLS),coreutil_$(t).cpp) coreutil_lib.cpp

gen/coreutil: gen/.stamp
	$(CXX) $(CXXFLAGS) $(addprefix gen/,$(COREUTIL_SRCS)) -o $@

# Trivial tools still compile from a single generated .cpp.
$(BINS): gen/.stamp
	$(CXX) $(CXXFLAGS) gen/$(@F).cpp -o $@

release:
	$(MAKE) CXXFLAGS="$(CXXFLAGS) $(RELEASE_FLAGS)"

debug:
	$(MAKE) CXXFLAGS="$(CXXFLAGS) $(DEBUG_FLAGS)"

clean:
	rm -rf gen
