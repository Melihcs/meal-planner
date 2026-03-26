export type Uuid = string;

export interface EntityDto {
  id: Uuid;
}

export interface TimestampedDto {
  createdAt: string;
  updatedAt?: string;
}
