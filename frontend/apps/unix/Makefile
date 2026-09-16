# Build all Myll Unix utilities in this directory.
#
# Run from this directory:
#   make
#   make clean

MAKEFLAGS += -j4

MYLL_ROOT ?= ../../..
MYLL      := dotnet run --project $(MYLL_ROOT)/frontend -- -i '*.myll' -o gen

CXX      ?= g++
CXXFLAGS ?= -std=c++23 -Ithird_party -DCXXOPTS_NO_REGEX -O1 -Wall -Wextra -Wsuggest-override

RELEASE_FLAGS ?= -O2 -s
DEBUG_FLAGS   ?= -Og -g

# Logic modules (coreutil_*.myll) and the combined dispatcher (coreutil.myll)
# do not produce standalone binaries by themselves.
SPLIT_TOOLS := find cat
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

# Pilot tools that are split into logic + standalone wrapper + shared library.
gen/find: gen/.stamp
	$(CXX) $(CXXFLAGS) gen/find.cpp gen/coreutil_find.cpp gen/coreutil_lib.cpp -o $@

gen/cat: gen/.stamp
	$(CXX) $(CXXFLAGS) gen/cat.cpp gen/coreutil_cat.cpp gen/coreutil_lib.cpp -o $@

# Combined multi-call binary: dispatcher + all tool logics + shared library.
gen/coreutil: gen/.stamp
	$(CXX) $(CXXFLAGS) gen/coreutil.cpp gen/coreutil_find.cpp gen/coreutil_cat.cpp gen/coreutil_lib.cpp -o $@

# Unconverted tools still compile from a single generated .cpp.
$(BINS): gen/.stamp
	$(CXX) $(CXXFLAGS) gen/$(@F).cpp -o $@

release:
	$(MAKE) CXXFLAGS="$(CXXFLAGS) $(RELEASE_FLAGS)"

debug:
	$(MAKE) CXXFLAGS="$(CXXFLAGS) $(DEBUG_FLAGS)"

clean:
	rm -rf gen
