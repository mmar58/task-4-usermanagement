<script lang="ts">
    import { onMount } from "svelte";
    import { page } from "$app/stores";
    import { api } from "$lib/api";
    import { Button } from "$lib/components/ui/button";
    import { CheckCircle2, XCircle, Loader2 } from "@lucide/svelte";
    import { goto } from "$app/navigation";

    let isLoading = $state(true);
    let isSuccess = $state(false);
    let message = $state("");
    let userName = $state("");

    onMount(async () => {
        const token = $page.url.searchParams.get("token");
        if (!token) {
            isLoading = false;
            isSuccess = false;
            message = "No verification token provided.";
            return;
        }

        try {
            const response = await api.post("/auth/verify", { token });
            isSuccess = true;
            message = response.data.message;
            userName = response.data.name;
        } catch (error: any) {
            isSuccess = false;
            message =
                error.response?.data?.message ||
                "Verification failed. The link might be expired or invalid.";
        } finally {
            isLoading = false;
        }
    });
</script>

<div
    class="min-h-screen bg-gray-50 flex flex-col justify-center items-center p-4"
>
    <div
        class="max-w-md w-full bg-white rounded-2xl shadow-xl overflow-hidden p-8 space-y-8 text-center"
    >
        {#if isLoading}
            <div class="flex flex-col items-center space-y-4">
                <Loader2 class="w-16 h-16 text-blue-500 animate-spin" />
                <h2 class="text-2xl font-semibold text-gray-900">
                    Verifying...
                </h2>
                <p class="text-gray-500">
                    Please wait while we verify your email address.
                </p>
            </div>
        {:else if isSuccess}
            <div
                class="flex flex-col items-center space-y-4 animate-in fade-in zoom-in duration-500"
            >
                <div
                    class="w-20 h-20 bg-green-100 rounded-full flex items-center justify-center mb-2"
                >
                    <CheckCircle2 class="w-12 h-12 text-green-600" />
                </div>
                <h1 class="text-3xl font-bold text-gray-900 tracking-tight">
                    Verified!
                </h1>
                <p class="text-lg text-gray-600">
                    Awesome, <span class="font-semibold text-gray-900"
                        >{userName}</span
                    >! Your email address has been successfully verified.
                </p>
                <div class="pt-6 w-full">
                    <Button
                        class="w-full h-12 text-lg bg-blue-600 hover:bg-blue-700 text-white rounded-xl shadow-lg transition-all hover:shadow-blue-500/25"
                        onclick={() => goto("/login")}
                    >
                        Go to Login
                    </Button>
                </div>
            </div>
        {:else}
            <div
                class="flex flex-col items-center space-y-4 animate-in fade-in zoom-in duration-500"
            >
                <div
                    class="w-20 h-20 bg-red-100 rounded-full flex items-center justify-center mb-2"
                >
                    <XCircle class="w-12 h-12 text-red-600" />
                </div>
                <h1 class="text-3xl font-bold text-gray-900 tracking-tight">
                    Verification Failed
                </h1>
                <p class="text-lg text-gray-600">{message}</p>
                <div class="pt-6 w-full">
                    <Button
                        variant="outline"
                        class="w-full h-12 text-lg rounded-xl"
                        on:click={() => goto("/login")}
                    >
                        Back to Login
                    </Button>
                </div>
            </div>
        {/if}
    </div>
</div>
